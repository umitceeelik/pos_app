using Microsoft.EntityFrameworkCore;
using Server.Domain.Entities;

namespace Server.Infrastructure;

/// <summary>
/// EF Core DbContext for the Hamam POS backend.
/// Maps domain entities to SQLite tables and configures indices/constraints.
/// </summary>
public class AppDb : DbContext
{
    /// <summary>Constructor that accepts DI-provided options.</summary>
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    // --- Tables (DbSets) ---
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionItem> SessionItems => Set<SessionItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>
    /// Configure schema details for each entity.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder b)
    {
        // ----- ROOM -----
        b.Entity<Room>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(64);
            e.Property(x => x.Status).IsRequired().HasMaxLength(16);

            // Default current timestamp (SQLite) and treat as a simple concurrency token
            e.Property(x => x.UpdatedAt)
             .HasDefaultValueSql("CURRENT_TIMESTAMP")
             .IsConcurrencyToken();

            // Optional: create an index on Status for quick filtering
            e.HasIndex(x => x.Status);
        });

        // ----- SESSION -----
        b.Entity<Session>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RoomId).IsRequired();
            e.Property(x => x.Status).IsRequired().HasMaxLength(16);

            // Helpful indices for queries by room & status
            e.HasIndex(x => x.RoomId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.StartAt);
        });

        // ----- SESSION ITEM -----
        b.Entity<SessionItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionId).IsRequired();
            e.Property(x => x.ServiceName).IsRequired().HasMaxLength(64);

            // Store money with precision; SQLite honors it via affinity
            e.Property(x => x.UnitPrice).HasColumnType("decimal(10,2)");

            e.HasIndex(x => x.SessionId);
            e.HasIndex(x => x.AddedAt);
        });

        // ----- PAYMENT -----
        b.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionId).IsRequired();
            e.Property(x => x.Method).IsRequired().HasMaxLength(8);
            e.Property(x => x.Amount).HasColumnType("decimal(10,2)");

            e.HasIndex(x => x.SessionId);
            e.HasIndex(x => x.PaidAt);
        });

        // (Optional) Relations: we keep it minimal to avoid cascade complexities in SQLite.
        // If you want to enforce FK constraints explicitly, you can configure them here.
        // Example:
        // b.Entity<Session>()
        //  .HasOne<Room>()
        //  .WithMany()
        //  .HasForeignKey(s => s.RoomId)
        //  .OnDelete(DeleteBehavior.Restrict);
    }
}
