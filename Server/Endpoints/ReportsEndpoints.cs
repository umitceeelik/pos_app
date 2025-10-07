using Microsoft.EntityFrameworkCore;
using Server.Application.Reports;
using Server.Infrastructure;

namespace Server.Endpoints;

/// <summary>
/// Reporting endpoints for simple business insights:
/// - Daily revenue summary (items total, payments total, by method)
/// - Room usage (session count & total minutes by room in a date range)
/// 
/// Notes:
/// * All timestamps are stored/treated as UTC.
/// * For SQLite, we do some computations in-memory after fetching compact sets.
/// </summary>
public static class ReportsEndpoints
{
    public static void MapReports(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/reports");

        // ------------------------------------------
        // GET /api/reports/daily?date=2025-10-07
        // Returns totals for the given date (00:00 - 23:59:59 UTC)
        // ------------------------------------------
        api.MapGet("daily", async (string? date, AppDb db, CancellationToken ct) =>
        {
            // Parse date; default to "today" (UTC) if not provided
            var d = string.IsNullOrWhiteSpace(date)
                ? DateOnly.FromDateTime(DateTime.UtcNow)
                : DateOnly.Parse(date);

            var from = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = d.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            // Items: load just today's items and sum per line (Qty × UnitPrice)
            var items = await db.SessionItems
                .AsNoTracking()
                .Where(i => i.AddedAt >= from && i.AddedAt <= to)
                .Select(i => new { i.Qty, i.UnitPrice })
                .ToListAsync(ct);

            var itemsTotal = items.Sum(i => i.UnitPrice * (decimal)i.Qty);

            // Payments: load today's payments, group by method
            var payments = await db.Payments
                .AsNoTracking()
                .Where(p => p.PaidAt >= from && p.PaidAt <= to)
                .Select(p => new { p.Method, p.Amount })
                .ToListAsync(ct);

            var paymentsTotal = payments.Sum(p => p.Amount);
            var byMethod = payments
                .GroupBy(p => p.Method)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var resp = new DailyRevenueResponse(
                d, itemsTotal, paymentsTotal, itemsTotal - paymentsTotal, byMethod);

            return Results.Ok(resp);
        })
        .WithSummary("Daily revenue summary")
        .WithDescription("Returns totals for a given date: items sum, payments sum, balance, and breakdown by method.");

        // --------------------------------------------------------------------
        // GET /api/reports/rooms/usage?from=2025-10-01T00:00:00Z&to=2025-10-07T23:59:59Z
        // Computes per-room session count and total minutes within [from, to].
        // If 'to' is omitted, uses UtcNow; if 'from' is omitted, uses 7 days ago.
        // Only sessions that have EndAt within range are counted.
        // --------------------------------------------------------------------
        api.MapGet("rooms/usage", async (string? from, string? to, AppDb db, CancellationToken ct) =>
        {
            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to).ToUniversalTime();
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-7) : DateTime.Parse(from).ToUniversalTime();

            // Bring back closed sessions in range with minimal columns
            var sessions = await db.Sessions
                .AsNoTracking()
                .Where(s => s.Status == "closed" && s.EndAt != null && s.EndAt >= fromUtc && s.EndAt <= toUtc)
                .Select(s => new { s.RoomId, s.StartAt, s.EndAt })
                .ToListAsync(ct);

            // Lookup room names once
            var roomMap = await db.Rooms.AsNoTracking()
                .Select(r => new { r.Id, r.Name })
                .ToDictionaryAsync(r => r.Id, r => r.Name, ct);

            // Aggregate in memory
            var rows = sessions
                .GroupBy(s => s.RoomId)
                .Select(g =>
                {
                    var minutes = g.Sum(s => (s.EndAt!.Value - s.StartAt).TotalMinutes);
                    return new RoomUsageRow(
                        RoomId: g.Key,
                        RoomName: roomMap.TryGetValue(g.Key, out var name) ? name : g.Key,
                        SessionsCount: g.Count(),
                        TotalMinutes: Math.Round(minutes, 1)
                    );
                })
                .OrderByDescending(r => r.TotalMinutes)
                .ToList();

            var resp = new RoomUsageResponse(fromUtc, toUtc, rows);
            return Results.Ok(resp);
        })
        .WithSummary("Room usage")
        .WithDescription("Returns per-room session count and total minutes within a UTC date range.");
    }
}
