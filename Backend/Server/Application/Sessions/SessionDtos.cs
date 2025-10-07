namespace Server.Application.Sessions;

/// <summary>Request to open a new session (adisyon) for a room.</summary>
public record OpenSessionRequest(
    string RoomId,
    string? CustomerName
);

/// <summary>Response returned by session endpoints.</summary>
public record SessionResponse(
    string Id,
    string RoomId,
    string? CustomerName,
    DateTime StartAt,
    DateTime? EndAt,
    string Status
);

/// <summary>Request to add a service/product line into a session.</summary>
public record AddItemRequest(
    string ServiceName,
    double Qty,
    decimal UnitPrice
);

/// <summary>Request to register a payment on a session.</summary>
public record AddPaymentRequest(
    string Method,   // "cash" | "card" | "mix"
    decimal Amount
);
