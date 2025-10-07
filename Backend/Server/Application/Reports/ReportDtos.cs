namespace Server.Application.Reports;

/// <summary>
/// Aggregated daily totals for a given calendar date (UTC).
/// </summary>
public record DailyRevenueResponse(
    DateOnly Date,
    decimal ItemsTotal,                 // Sum of all session items' totals
    decimal PaymentsTotal,              // Sum of all payments
    decimal Balance,                    // ItemsTotal - PaymentsTotal
    IDictionary<string, decimal> PaymentsByMethod // e.g., { "cash": 1200, "card": 800 }
);

/// <summary>
/// Room usage statistics between a date range (UTC).
/// </summary>
public record RoomUsageRow(
    string RoomId,
    string RoomName,
    int SessionsCount,                  // Number of sessions (closed within range)
    double TotalMinutes                 // Sum of session durations in minutes
);

/// <summary>
/// Wrapper for room usage response.
/// </summary>
public record RoomUsageResponse(
    DateTime FromUtc,
    DateTime ToUtc,
    IEnumerable<RoomUsageRow> Rows
);
