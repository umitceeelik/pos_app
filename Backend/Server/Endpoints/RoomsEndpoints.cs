using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Common;           // ValidateRequestFilter<T>
using Server.Domain.Entities;
using Server.Hubs;             // RoomsHub
using Server.Infrastructure;   // AppDb

namespace Server.Endpoints;

/// <summary>
/// REST endpoints for Room operations (list/create/update).
/// Also broadcasts real-time updates to all clients via SignalR.
/// </summary>
public static class RoomsEndpoints
{
    /// <summary>
    /// Maps room endpoints under "/api/rooms".
    /// </summary>
    public static void MapRooms(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/rooms");

        // -----------------------------
        // GET /api/rooms
        // Returns all rooms ordered by name
        // -----------------------------
        api.MapGet("", async (AppDb db, CancellationToken ct) =>
        {
            var rooms = await db.Rooms
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            return Results.Ok(rooms);
        })
        .WithSummary("List rooms")
        .WithDescription("Returns all rooms ordered by name.");

        // -----------------------------
        // POST /api/rooms
        // Create a new room
        // -----------------------------
        api.MapPost("", async (AppDb db, IHubContext<RoomsHub> hub, Room req, CancellationToken ct) =>
        {
            // Basic server-side normalization
            req.Id = string.IsNullOrWhiteSpace(req.Id) ? Guid.NewGuid().ToString() : req.Id;
            req.Name = req.Name.Trim();
            req.Status = string.IsNullOrWhiteSpace(req.Status) ? "available" : req.Status.Trim();
            req.UpdatedAt = DateTime.UtcNow;

            db.Rooms.Add(req);
            await db.SaveChangesAsync(ct);

            // Broadcast to all connected clients
            await hub.Clients.All.SendAsync("RoomUpdated", req.Id, req.Status, ct);

            return Results.Created($"/api/rooms/{req.Id}", req);
        })
        .WithSummary("Create room")
        .WithDescription("Creates a new room and broadcasts a RoomUpdated event.");

        // -----------------------------
        // PUT /api/rooms/{id}
        // Update existing room (name/status)
        // -----------------------------
        api.MapPut("{id}", async (string id, AppDb db, IHubContext<RoomsHub> hub, Room req, CancellationToken ct) =>
        {
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (room is null) return Results.NotFound(new { message = "Room not found." });

            room.Name = req.Name?.Trim() ?? room.Name;
            room.Status = string.IsNullOrWhiteSpace(req.Status) ? room.Status : req.Status.Trim();
            room.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            await hub.Clients.All.SendAsync("RoomUpdated", room.Id, room.Status, ct);
            return Results.Ok(room);
        })
        .WithSummary("Update room")
        .WithDescription("Updates the given room and broadcasts a RoomUpdated event.");
    }
}
