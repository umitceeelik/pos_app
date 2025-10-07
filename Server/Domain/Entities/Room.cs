namespace Server.Domain.Entities;

/// <summary>
/// Represents a physical room in the Hamam (bathhouse).
/// Each room can be assigned for different purposes (hot room, massage, scrub, etc.).
/// </summary>
public class Room
{
    /// <summary>Unique identifier (GUID as string).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Display name (e.g., "Hot Room 1").</summary>
    public string Name { get; set; } = default!;

    /// <summary>Current status: "available" | "occupied" | "cleaning" | "maintenance".</summary>
    public string Status { get; set; } = "available";

    /// <summary>Last update timestamp in UTC. Also used as a simple concurrency token.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
