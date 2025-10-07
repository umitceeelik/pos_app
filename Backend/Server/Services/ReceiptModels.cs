namespace Server.Services;

/// <summary>
/// A single line on the receipt (service/product).
/// </summary>
public record ReceiptLine(string Name, double Qty, decimal UnitPrice)
{
    public decimal Total => (decimal)Qty * UnitPrice;
}

/// <summary>
/// All data required to render/print a receipt.
/// </summary>
public record ReceiptData(
    string Title,
    string BusinessName,
    string RoomName,
    string SessionId,
    DateTime StartAt,
    DateTime EndAt,
    IEnumerable<ReceiptLine> Lines,
    decimal ItemsTotal,
    IEnumerable<(string Method, decimal Amount)> Payments,
    decimal PaymentsTotal,
    decimal Balance
);
