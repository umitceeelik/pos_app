namespace Server.Domain.Entities;

/// <summary>
/// A service/product line inside a session (e.g., "Massage 30min").
/// </summary>
public class SessionItem
{
    /// <summary>Unique identifier (GUID as string).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>FK to the parent <see cref="Session"/>.</summary>
    public string SessionId { get; set; } = default!;

    /// <summary>Service or item name (e.g., "Scrub", "Tea").</summary>
    public string ServiceName { get; set; } = default!;

    /// <summary>Quantity of the service/product.</summary>
    public double Qty { get; set; } = 1;

    /// <summary>Unit price (decimal to avoid floating-point rounding issues).</summary>
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>
    /// Convenience computed total (Qty × UnitPrice).
    /// NOTE: Not mapped to DB; do not use in LINQ-to-Entities aggregations.
    /// </summary>
    public decimal Total => UnitPrice * (decimal)Qty;

    /// <summary>UTC time when the item was added.</summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
