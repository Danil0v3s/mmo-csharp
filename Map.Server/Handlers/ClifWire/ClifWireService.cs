using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Handlers.ClifWire;

/// <summary>
/// Default <see cref="IClifWireService"/>. Wire packets live with
/// their handlers; entries here log so the caller has a named place
/// to land while the per-packet emitter wires in.
/// </summary>
public sealed class ClifWireService : IClifWireService
{
    private readonly ILogger<ClifWireService> _logger;
    public ClifWireService(ILogger<ClifWireService> logger) => _logger = logger;

    public void MessageColor(PlayerEntity pc, uint colorRgb, string text)
        => _logger.LogDebug("clif_messagecolor pc={Pc} #{Color:X6} {Text}", pc.Id, colorRgb, text);
    public void StatusChange(Entity target, Map.Server.Status.StatusType type, bool active,
        int totalMs = 0, int val1 = 0, int val2 = 0, int val3 = 0)
    {
        // T5.3b — rAthena clif_status_change (clif.cpp:9852) +
        // clif_efst_status_change. The wire packet is one of
        // ZC_MSG_STATE_CHANGE (0x0196) / ZC_MSG_STATE_CHANGE2 (0x043f) /
        // ZC_EFST_STATUS_CHANGE (0x0983) depending on PACKETVER. We
        // log here so consumers can wire the canonical name; the
        // per-packet OutgoingPacket emitter lands when the live
        // client's status-icon set is mapped end-to-end.
        _logger.LogDebug(
            "clif_status_change target={Target} sc={Sc} active={Active} ms={Ms} val1={V1} val2={V2} val3={V3}",
            target.Id, type, active, totalMs, val1, val2, val3);
    }

    public void MobChat(MobEntity mob, uint colorRgb, string text)
    {
        // rAthena mob.cpp:4210-4217 — drop everything after the first
        // '#' in the mob's name (Aegis-style "Poring#room_1" → "Poring")
        // then format "<name> : <text>" and broadcast in AREA_CHAT_WOC.
        // Until the AOI broadcaster lands the canonical naming seam
        // just logs; switch to clif_messagecolor on every PC in view.
        var name = mob.Name ?? string.Empty;
        var hash = name.IndexOf('#');
        if (hash >= 0) name = name.Substring(0, hash);
        _logger.LogDebug("clif_mobchat mob={Mob} #{Color:X6} {Name} : {Text}",
            mob.Id, colorRgb, name, text);
    }
    public void DisplayMessage(PlayerEntity pc, string text)
        => _logger.LogDebug("clif_displaymessage pc={Pc} {Text}", pc.Id, text);
    public void Broadcast(string text, uint colorRgb, byte type)
        => _logger.LogInformation("clif_broadcast {Text}", text);
    public void Broadcast2(uint mapId, string text, uint colorRgb, byte type)
        => _logger.LogInformation("clif_broadcast2 map={Map} {Text}", mapId, text);
    public void Refresh(PlayerEntity pc) { }
    public void ChangeMap(PlayerEntity pc, string mapName, short x, short y) { }
    public void ClearUnitSingle(EntityId targetId, byte type, PlayerEntity audience) { }
    public void ClearUnitArea(Entity target, byte type) { }
    public void AuthOk(PlayerEntity pc) { }
    public void AuthRefuse(int reason) { }
    public void AuthFailFd(int fd, byte reason) { }
}
