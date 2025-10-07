namespace Server.Domain.Entities;

/// <summary>
/// Payment recorded for a session. Multiple payments are supported.
/// </summary>
public class Payment
{
    /// <summary>Unique identifier (GUID as string).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>FK to the related <see cref="Session"/>.</summary>
    public string SessionId { get; set; } = default!;

    /// <summary>Payment method: "cash" | "card" | "mix".</summary>
    public string Method { get; set; } = "cash";

    /// <summary>Amount paid.</summary>
    public decimal Amount { get; set; } = 0m;

    /// <summary>UTC time when the payment was made.</summary>
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
