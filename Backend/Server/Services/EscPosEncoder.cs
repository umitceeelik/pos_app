using System.Text;

namespace Server.Services;

/// <summary>
/// Minimal ESC/POS encoder (Turkish-friendly via CP857).
/// Converts a ReceiptData to ESC/POS byte stream for thermal printers.
/// Expand with QR, barcode, double-width fonts, etc. as needed.
/// </summary>
public static class EscPosEncoder
{
    private static readonly byte[] Init = { 0x1B, 0x40 };                 // Initialize
    private static readonly byte[] Cut = { 0x1D, 0x56, 0x41, 0x10 };     // Partial cut
    private static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };
    private static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };
    private static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };
    private static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };

    // Many printers expect CP857/CP1254 for Turkish
    private static readonly Encoding Tr = Encoding.GetEncoding(857);

    public static byte[] Build(ReceiptData r)
    {
        using var ms = new MemoryStream();
        void W(params byte[][] chunks) { foreach (var c in chunks) ms.Write(c, 0, c.Length); }
        void WL(string s) { var b = Tr.GetBytes(s + "\n"); ms.Write(b, 0, b.Length); }

        W(Init, AlignCenter, BoldOn);
        WL(r.Title);
        WL(r.BusinessName);
        W(BoldOff);
        WL(new string('-', 40));

        W(AlignLeft);
        WL($"Room   : {r.RoomName}");
        WL($"Ticket : {r.SessionId}");
        WL($"Start  : {r.StartAt:yyyy-MM-dd HH:mm}");
        WL($"End    : {r.EndAt:yyyy-MM-dd HH:mm}");
        WL(new string('-', 40));

        foreach (var l in r.Lines)
        {
            WL(l.Name);
            WL($"{l.Qty} x {l.UnitPrice:0.##}".PadRight(28) + $"{l.Total:0.##}".PadLeft(12));
        }

        WL(new string('-', 40));
        WL($"TOTAL".PadRight(28) + $"{r.ItemsTotal:0.##}".PadLeft(12));

        foreach (var p in r.Payments)
            WL(($"Pay {p.Method}").PadRight(28) + $"{p.Amount:0.##}".PadLeft(12));

        if (r.Payments.Any())
            WL(("Payments").PadRight(28) + $"{r.PaymentsTotal:0.##}".PadLeft(12));

        WL(new string('-', 40));
        WL(("Balance").PadRight(28) + $"{r.Balance:0.##}".PadLeft(12));

        W(AlignCenter);
        WL(""); WL("Thank you!"); WL("");
        W(Cut);

        return ms.ToArray();
    }
}
