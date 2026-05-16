using Core.Server.IPC;

namespace Char.Server.Services;

/// <summary>
/// Char-side IPC wrapper for pushing inter-base notifications to map servers.
/// Provides fan-out (all maps) for broadcast and directed (specific map) for whisper.
/// Mirrors rAthena's chmapif_sendall / chmapif_send_msg flows for inter.cpp.
/// </summary>
public interface IMapServerIpcService
{
    /// <summary>Fan out a server-wide broadcast to every connected map server.</summary>
    Task BroadcastAsync(MapBroadcastNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Fan out an item-drop broadcast to every connected map server.</summary>
    Task BroadcastItemAsync(MapItemBroadcastNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deliver a whisper. Fans out to all map servers; each replies with delivered=true
    /// if the target is on that map. Returns true if any map reported delivery.
    /// </summary>
    Task<bool> SendWhisperAsync(MapWhisperNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Fan out a whisper-to-GM to all map servers; each filters by group_id locally.</summary>
    Task SendWhisperToGmAsync(MapWhisperToGmNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Fan out an entity name change so each map server can refresh its UI.</summary>
    Task NotifyNameChangeAsync(MapNameChangeNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Fan out an address-sync request so each map server re-resolves its own address.</summary>
    Task NotifyAddressSyncAsync(CancellationToken cancellationToken = default);
}
