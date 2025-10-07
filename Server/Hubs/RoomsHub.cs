using Microsoft.AspNetCore.SignalR;

namespace Server.Hubs;

/// <summary>
/// SignalR Hub for real-time updates to all connected clients.
/// Broadcast events used by the Flutter UI:
/// - "RoomUpdated"   (roomId, status)
/// - "SessionOpened" (sessionId)
/// - "SessionUpdated"(sessionId)
/// - "SessionClosed" (sessionId)
/// </summary>
public class RoomsHub : Hub
{
    // Keep hub minimal; we push events from endpoints via IHubContext<RoomsHub>.
    // If you need server-invokable methods from clients, add them here.
}
