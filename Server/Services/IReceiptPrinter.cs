namespace Server.Services;

/// <summary>
/// Strategy interface for receipt printing.
/// Implementations: Preview (files), LAN (ESC/POS over TCP), USB (raw).
/// </summary>
public interface IReceiptPrinter
{
    Task PrintAsync(ReceiptData data, CancellationToken ct = default);
}
