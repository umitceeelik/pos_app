using System.Text;

namespace Server.Services;

/// <summary>
/// Simple plain-text formatter for receipts (40-char width ticket).
/// Useful for PREVIEW mode and debugging.
/// </summary>
public static class ReceiptFormatter
{
    public static string ToPlainText(ReceiptData r)
    {
        const int width = 40;
        var sb = new StringBuilder();

        sb.AppendLine(Center(r.Title, width));
        sb.AppendLine(Center(r.BusinessName, width));
        sb.AppendLine(new string('-', width));
        sb.AppendLine($"Room   : {r.RoomName}");
        sb.AppendLine($"Ticket : {r.SessionId}");
        sb.AppendLine($"Start  : {r.StartAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"End    : {r.EndAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine(new string('-', width));

        foreach (var l in r.Lines)
        {
            sb.AppendLine(l.Name);
            var left = $"{l.Qty} x {l.UnitPrice:0.##}";
            var right = $"{l.Total:0.##}";
            sb.AppendLine(Align(left, right, width));
        }

        sb.AppendLine(new string('-', width));
        sb.AppendLine(Align("Items Total", $"{r.ItemsTotal:0.##}", width));

        foreach (var p in r.Payments)
            sb.AppendLine(Align($"Pay {p.Method}", $"{p.Amount:0.##}", width));

        if (r.Payments.Any())
            sb.AppendLine(Align("Payments Total", $"{r.PaymentsTotal:0.##}", width));

        sb.AppendLine(new string('-', width));
        sb.AppendLine(Align("Balance", $"{r.Balance:0.##}", width));
        sb.AppendLine(new string('-', width));
        sb.AppendLine(Center("Thank you!", width));
        sb.AppendLine(); sb.AppendLine(); // some extra feed
        return sb.ToString();
    }

    private static string Center(string s, int w)
    {
        s = s.Trim();
        if (s.Length >= w) return s;
        var pad = (w - s.Length) / 2;
        return new string(' ', pad) + s;
    }

    private static string Align(string left, string right, int w)
    {
        left = left.Trim(); right = right.Trim();
        var spaces = Math.Max(1, w - left.Length - right.Length);
        return left + new string(' ', spaces) + right;
    }
}
