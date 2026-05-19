using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Stub-grade <see cref="IPlayerBonusService"/>. The full
/// implementation needs the bonus-script runtime that interprets
/// rAthena's pc_bonus opcode set. Until then this service:
/// <list type="bullet">
///   <item>Logs every call so QA sees what gets requested.</item>
///   <item>Returns success/failure consistent with rAthena's
///         contract so callers don't need to special-case the
///         "engine pending" state.</item>
/// </list>
/// </summary>
public sealed class PlayerBonusService : IPlayerBonusService
{
    private readonly ILogger<PlayerBonusService> _logger;
    public PlayerBonusService(ILogger<PlayerBonusService> logger) => _logger = logger;

    public bool AddBonusScript(PlayerEntity pc, string script, int durationMs, ushort iconType, bool persistent)
    {
        _logger.LogDebug(
            "pc_bonus_script stub: char {Char} duration {Dur}ms icon {Icon} persistent={Persist}\n  {Script}",
            pc.CharacterId, durationMs, iconType, persistent, script);
        return true;
    }

    public void ClearBonusScripts(PlayerEntity pc, int flag)
    {
        _logger.LogDebug("pc_bonus_script_clear stub: char {Char} flag {Flag:X}", pc.CharacterId, flag);
    }

    public bool AddAutobonus(PlayerEntity pc, AutobonusTrigger trigger, string script, int rate, int durationMs, ushort flag)
    {
        _logger.LogDebug(
            "pc_addautobonus stub: char {Char} trigger {Trigger} rate {Rate}/10000 dur {Dur}ms",
            pc.CharacterId, trigger, rate, durationMs);
        return true;
    }

    public void DelAutobonus(PlayerEntity pc, AutobonusTrigger trigger, bool restore)
    {
        _logger.LogDebug(
            "pc_delautobonus stub: char {Char} trigger {Trigger} restore={Restore}",
            pc.CharacterId, trigger, restore);
    }

    public void ExecuteAutobonus(PlayerEntity pc, AutobonusTrigger trigger)
    {
        // Combat / skill paths can call this freely; until the
        // engine ports, exec is a no-op.
    }
}
