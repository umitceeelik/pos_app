using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Server; // AppSettings

namespace Server.Services;

/// <summary>
/// ESC/POS over TCP (port 9100 typically) printer implementation.
/// Set Printer.Mode = "LAN" in appsettings to use this printer.
/// </summary>
public class EscPosTcpPrinter : IReceiptPrinter
{
    private readonly PrinterOptions _opts;
    public EscPosTcpPrinter(IOptions<AppSettings> options) => _opts = options.Value.Printer;

    public async Task PrintAsync(ReceiptData data, CancellationToken ct = default)
    {
        var bytes = EscPosEncoder.Build(data);
        using var client = new TcpClient();
        await client.ConnectAsync(_opts.Host, _opts.Port);
        using var stream = client.GetStream();
        await stream.WriteAsync(bytes, 0, bytes.Length, ct);
        await stream.FlushAsync(ct);
    }
}
