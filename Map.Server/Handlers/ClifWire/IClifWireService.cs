using Map.Server.Entities;

namespace Map.Server.Handlers.ClifWire;

/// <summary>
/// Outbound-packet builders. Canonical entry points for the rAthena
/// <c>clif.cpp</c> public surface (25 817 lines, 780 enumerated
/// public functions — almost all of them are `clif_*` packet emitters
/// like <c>clif_charnameack</c>, <c>clif_party_xy</c>, etc.).
///
/// The C# port handles outbound packets through
/// <see cref="Core.Server.Packets.OutgoingPacket"/> + per-packet
/// emitter classes. This service is the *canonical naming seam* —
/// rAthena consumers (skills / scripts / atcommands) that say
/// "send a clif_status_change to X" have a single named method to
/// call.
///
/// The interface is intentionally minimal — most clif_* methods are
/// either:
/// <list type="bullet">
///   <item>already covered by a dedicated `Send*Packet` helper, or</item>
///   <item>fired by a packet emitter that owns its own send loop.</item>
/// </list>
/// New entry points get added when the matching consumer (the skill
/// system, the chat router, the trade engine) needs one.
/// </summary>
public interface IClifWireService
{
    /// <summary>rAthena <c>clif_messagecolor</c> — colored system message.</summary>
    void MessageColor(PlayerEntity pc, uint colorRgb, string text);

    /// <summary>rAthena <c>clif_displaymessage</c> — single-line message in chat.</summary>
    void DisplayMessage(PlayerEntity pc, string text);

    /// <summary>rAthena <c>clif_broadcast</c> — server-wide announcement.</summary>
    void Broadcast(string text, uint colorRgb, byte type);

    /// <summary>rAthena <c>clif_broadcast2</c> — map-scoped announcement.</summary>
    void Broadcast2(uint mapId, string text, uint colorRgb, byte type);

    /// <summary>rAthena <c>clif_refresh</c> — re-send the full PC state to its own client.</summary>
    void Refresh(PlayerEntity pc);

    /// <summary>rAthena <c>clif_changemap</c> — map-warp packet.</summary>
    void ChangeMap(PlayerEntity pc, string mapName, short x, short y);

    /// <summary>rAthena <c>clif_clearunit_single</c> — despawn a single entity from one client.</summary>
    void ClearUnitSingle(EntityId targetId, byte type, PlayerEntity audience);

    /// <summary>rAthena <c>clif_clearunit_area</c> — despawn an entity from everyone in range.</summary>
    void ClearUnitArea(Entity target, byte type);

    /// <summary>rAthena <c>clif_authok</c> — connection accepted.</summary>
    void AuthOk(PlayerEntity pc);

    /// <summary>rAthena <c>clif_authrefuse</c>.</summary>
    void AuthRefuse(int reason);

    /// <summary>rAthena <c>clif_authfail_fd</c>.</summary>
    void AuthFailFd(int fd, byte reason);
}
