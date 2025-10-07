using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Application.Sessions; // DTOs (OpenSessionRequest, AddItemRequest, etc.)
using Server.Common;               // ValidateRequestFilter<T>
using Server.Domain.Entities;
using Server.Hubs;                 // RoomsHub
using Server.Infrastructure;       // AppDb
using Server.Services;             // IReceiptPrinter

namespace Server.Endpoints;

/// <summary>
/// REST endpoints covering the full lifecycle of a session (adisyon):
/// - Open session for a room
/// - Add items & payments
/// - Close session (with receipt printing)
/// - Query active sessions / get by id with totals
/// Integrated with SignalR for real-time UI updates.
/// </summary>
public static class SessionsEndpoints
{
    /// <summary>
    /// Maps session endpoints under "/api/sessions".
    /// </summary>
    public static void MapSessions(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/sessions");

        // -----------------------------
        // GET /api/sessions/active
        // List currently open sessions (adisyons)
        // -----------------------------
        api.MapGet("active", async (AppDb db, CancellationToken ct) =>
        {
            var list = await db.Sessions
                .AsNoTracking()
                .Where(s => s.Status == "open")
                .OrderByDescending(s => s.StartAt)
                .ToListAsync(ct);

            var result = list.Select(s => new SessionResponse(
                s.Id, s.RoomId, s.CustomerName, s.StartAt, s.EndAt, s.Status));

            return Results.Ok(result);
        })
        .WithSummary("List active sessions")
        .WithDescription("Returns all sessions with status 'open' ordered by start time.");

        // -----------------------------
        // GET /api/sessions/{id}
        // Get session with items/payments and totals
        // -----------------------------
        api.MapGet("{id}", async (string id, AppDb db, CancellationToken ct) =>
        {
            var s = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return Results.NotFound(new { message = "Session not found." });

            var items = await db.SessionItems.AsNoTracking().Where(x => x.SessionId == id).ToListAsync(ct);
            var pays = await db.Payments.AsNoTracking().Where(x => x.SessionId == id).ToListAsync(ct);

            var itemsTotal = items.Sum(i => i.UnitPrice * (decimal)i.Qty); // compute in-memory
            var paysTotal = pays.Sum(p => p.Amount);
            var balance = itemsTotal - paysTotal;

            return Results.Ok(new
            {
                session = new SessionResponse(s.Id, s.RoomId, s.CustomerName, s.StartAt, s.EndAt, s.Status),
                items,
                payments = pays,
                totals = new { itemsTotal, paymentsTotal = paysTotal, balance }
            });
        })
        .WithSummary("Get session details")
        .WithDescription("Returns a session with its items, payments and computed totals.");

        // -----------------------------
        // POST /api/sessions/open
        // Open a new session for a room (marks room as occupied)
        // -----------------------------
        api.MapPost("open", async (AppDb db, IHubContext<RoomsHub> hub, OpenSessionRequest req, CancellationToken ct) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var room = await db.Rooms.FindAsync(new object?[] { req.RoomId }, ct);
            if (room is null) return Results.BadRequest(new { message = "Room not found." });

            var alreadyOpen = await db.Sessions.AnyAsync(s => s.RoomId == req.RoomId && s.Status == "open", ct);
            if (alreadyOpen) return Results.BadRequest(new { message = "There is already an open session in this room." });

            var session = new Session
            {
                RoomId = req.RoomId,
                CustomerName = string.IsNullOrWhiteSpace(req.CustomerName) ? null : req.CustomerName.Trim(),
                StartAt = DateTime.UtcNow,
                Status = "open"
            };

            room.Status = "occupied";
            room.UpdatedAt = DateTime.UtcNow;

            db.Sessions.Add(session);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Realtime notifications
            await hub.Clients.All.SendAsync("RoomUpdated", room.Id, room.Status, ct);
            await hub.Clients.All.SendAsync("SessionOpened", session.Id, ct);

            var response = new SessionResponse(session.Id, session.RoomId, session.CustomerName, session.StartAt, session.EndAt, session.Status);
            return Results.Created($"/api/sessions/{session.Id}", response);
        })
        .AddEndpointFilter<ValidateRequestFilter<OpenSessionRequest>>() // run FluentValidation
        .WithSummary("Open session")
        .WithDescription("Opens a session for a room and sets the room status to 'occupied'.");

        // -----------------------------
        // POST /api/sessions/{id}/items
        // Add a service/product line to a session
        // -----------------------------
        api.MapPost("{id}/items", async (string id, AppDb db, IHubContext<RoomsHub> hub, AddItemRequest req, CancellationToken ct) =>
        {
            var s = await db.Sessions.FindAsync(new object?[] { id }, ct);
            if (s is null) return Results.NotFound(new { message = "Session not found." });
            if (s.Status != "open") return Results.BadRequest(new { message = "Session is not open." });

            var item = new SessionItem
            {
                SessionId = s.Id,
                ServiceName = req.ServiceName.Trim(),
                Qty = req.Qty,
                UnitPrice = req.UnitPrice,
                AddedAt = DateTime.UtcNow
            };

            db.SessionItems.Add(item);
            await db.SaveChangesAsync(ct);

            // Notify clients to refresh totals
            await hub.Clients.All.SendAsync("SessionUpdated", s.Id, ct);

            return Results.Created($"/api/sessions/{s.Id}/items/{item.Id}", item);
        })
        .AddEndpointFilter<ValidateRequestFilter<AddItemRequest>>() // run FluentValidation
        .WithSummary("Add session item")
        .WithDescription("Adds a service/product line to an open session and notifies clients.");

        // -----------------------------
        // POST /api/sessions/{id}/payments
        // Add a payment to a session
        // -----------------------------
        api.MapPost("{id}/payments", async (string id, AppDb db, IHubContext<RoomsHub> hub, AddPaymentRequest req, CancellationToken ct) =>
        {
            var s = await db.Sessions.FindAsync(new object?[] { id }, ct);
            if (s is null) return Results.NotFound(new { message = "Session not found." });
            if (s.Status != "open") return Results.BadRequest(new { message = "Session is not open." });

            var payment = new Payment
            {
                SessionId = s.Id,
                Method = req.Method.Trim().ToLowerInvariant(),
                Amount = req.Amount,
                PaidAt = DateTime.UtcNow
            };

            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);

            await hub.Clients.All.SendAsync("SessionUpdated", s.Id, ct);
            return Results.Created($"/api/sessions/{s.Id}/payments/{payment.Id}", payment);
        })
        .AddEndpointFilter<ValidateRequestFilter<AddPaymentRequest>>() // run FluentValidation
        .WithSummary("Add payment")
        .WithDescription("Registers a payment for an open session and notifies clients.");

        // -----------------------------
        // POST /api/sessions/{id}/close
        // Close session if balance is zero; print receipt via injected IReceiptPrinter
        // -----------------------------
        api.MapPost("{id}/close", async (string id, AppDb db, IHubContext<RoomsHub> hub, IReceiptPrinter printer, CancellationToken ct) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var s = await db.Sessions.FindAsync(new object?[] { id }, ct);
            if (s is null) return Results.NotFound(new { message = "Session not found." });
            if (s.Status != "open") return Results.BadRequest(new { message = "Session is not open." });

            // Load items/payments to compute totals
            var items = await db.SessionItems.Where(i => i.SessionId == s.Id).ToListAsync(ct);
            var pays = await db.Payments.Where(p => p.SessionId == s.Id).ToListAsync(ct);

            // IMPORTANT: compute totals from columns (not from computed property)
            var itemsTotal = items.Sum(i => i.UnitPrice * (decimal)i.Qty);
            var paysTotal = pays.Sum(p => p.Amount);
            var balance = itemsTotal - paysTotal;

            if (balance != 0)
                return Results.BadRequest(new { message = "Cannot close session. Balance must be zero.", itemsTotal, paymentsTotal = paysTotal, balance });

            // Close session & free room
            s.EndAt = DateTime.UtcNow;
            s.Status = "closed";

            var room = await db.Rooms.FindAsync(new object?[] { s.RoomId }, ct);
            if (room is not null)
            {
                room.Status = "available";
                room.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Build receipt & print AFTER commit (avoid side effects within transaction)
            var data = new Server.Services.ReceiptData(
                Title: "HAMAM POS RECEIPT",
                BusinessName: "Your Business Ltd.",
                RoomName: room?.Name ?? s.RoomId,
                SessionId: s.Id,
                StartAt: s.StartAt,
                EndAt: s.EndAt ?? DateTime.UtcNow,
                Lines: items.Select(i => new Server.Services.ReceiptLine(i.ServiceName, i.Qty, i.UnitPrice)),
                ItemsTotal: itemsTotal,
                Payments: pays.Select(p => (p.Method.ToUpperInvariant(), p.Amount)),
                PaymentsTotal: paysTotal,
                Balance: balance
            );
            await printer.PrintAsync(data, ct);

            // Realtime notifications
            await hub.Clients.All.SendAsync("SessionClosed", s.Id, ct);
            if (room is not null)
                await hub.Clients.All.SendAsync("RoomUpdated", room.Id, room.Status, ct);

            var response = new SessionResponse(s.Id, s.RoomId, s.CustomerName, s.StartAt, s.EndAt, s.Status);
            return Results.Ok(response);
        })
        .WithSummary("Close session")
        .WithDescription("Closes a session (requires zero balance), prints receipt, and notifies clients.");
    }
}
