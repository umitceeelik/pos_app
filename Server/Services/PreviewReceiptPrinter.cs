using System.Text;
using Microsoft.Extensions.Options;
using Server; // AppSettings

namespace Server.Services;

/// <summary>
/// PREVIEW printer: writes .txt and .escpos files to disk instead of printing.
/// Great for development and testing without a physical printer.
/// </summary>
public class PreviewReceiptPrinter : IReceiptPrinter
{
    private readonly PrinterOptions _opts;
    public PreviewReceiptPrinter(IOptions<AppSettings> options)
        => _opts = options.Value.Printer;

    public async Task PrintAsync(ReceiptData data, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_opts.PreviewOutputDir);

        // 1) Human-readable text ticket
        var txt = ReceiptFormatter.ToPlainText(data);
        var txtPath = Path.Combine(_opts.PreviewOutputDir, $"{Sanitize(data.SessionId)}.txt");
        await File.WriteAllTextAsync(txtPath, txt, Encoding.UTF8, ct);

        // 2) Raw bytes placeholder for ESC/POS (for now we just reuse the text)
        var escPath = Path.Combine(_opts.PreviewOutputDir, $"{Sanitize(data.SessionId)}.escpos");
        await File.WriteAllBytesAsync(escPath, Encoding.UTF8.GetBytes(txt), ct);

        Console.WriteLine($"[PREVIEW] Receipt saved: {txtPath}");
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }
}
