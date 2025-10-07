namespace Server.Domain.Entities;

/// <summary>
/// Represents a customer session (adisyon) inside a room.
/// Tracks start/end time and lifecycle: open, closed, or cancelled.
/// </summary>
public class Session
{
    /// <summary>Unique identifier (GUID as string).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>FK to <see cref="Room"/> this session belongs to.</summary>
    public string RoomId { get; set; } = default!;

    /// <summary>Optional customer name (can be empty).</summary>
    public string? CustomerName { get; set; }

    /// <summary>UTC time when the session started.</summary>
    public DateTime StartAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC time when the session ended (null while open).</summary>
    public DateTime? EndAt { get; set; }

    /// <summary>Lifecycle state: "open" | "closed" | "cancelled".</summary>
    public string Status { get; set; } = "open";
}
