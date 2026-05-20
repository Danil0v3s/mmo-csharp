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
