using Map.Server.Combat;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Stub <see cref="IPlayerAttendanceService"/>. Will load
/// db/re/attendance.yml + track per-account last-claim day when the
/// data side ports.
/// </summary>
public sealed class PlayerAttendanceService : IPlayerAttendanceService
{
    private readonly ILogger<PlayerAttendanceService> _logger;
    public PlayerAttendanceService(ILogger<PlayerAttendanceService> logger) => _logger = logger;
    public bool IsEnabled() => false;
    public AttendanceClaimResult Claim(PlayerEntity pc, int day)
    {
        _logger.LogDebug("pc_attendance_claim_reward stub: char {Char} day {Day}", pc.CharacterId, day);
        return AttendanceClaimResult.NotEnabled;
    }
}

/// <summary>Stub <see cref="IPlayerQuestMarkerService"/>.</summary>
public sealed class PlayerQuestMarkerService : IPlayerQuestMarkerService
{
    public void Refresh(PlayerEntity pc) { }
    public void Reinit(PlayerEntity pc) { }
}

/// <summary>Stub <see cref="IPlayerStealService"/> — Steal skill backend pending.</summary>
public sealed class PlayerStealService : IPlayerStealService
{
    private readonly ILogger<PlayerStealService> _logger;
    public PlayerStealService(ILogger<PlayerStealService> logger) => _logger = logger;
    public bool TrySteal(PlayerEntity thief, MobEntity target)
    {
        _logger.LogDebug("pc_steal_item stub: char {Char} vs mob {Mob}", thief.CharacterId, target.Id.Value);
        return false;
    }
}

/// <summary>Stub <see cref="IPlayerReputationService"/>.</summary>
public sealed class PlayerReputationService : IPlayerReputationService
{
    public void Generate(PlayerEntity pc) { }
}

/// <summary>
/// <see cref="IPlayerVersionDisplayService"/> — pipes the running
/// assembly version through ZC_NOTIFY_PLAYERCHAT.
/// </summary>
public sealed class PlayerVersionDisplayService : IPlayerVersionDisplayService
{
    private readonly Visibility.IVisibilityService _visibility;
    public PlayerVersionDisplayService(Visibility.IVisibilityService visibility) => _visibility = visibility;
    public void Show(PlayerEntity pc)
    {
        var ver = typeof(MapServerImpl).Assembly.GetName().Version?.ToString() ?? "dev";
        _visibility.SendToSelf(pc, new Core.Server.Packets.Out.ZC.ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"Map.Server (C#) — {ver}",
        });
    }
}

/// <summary>
/// <see cref="IPlayerReviveItemService"/>. Token of Siegfried (id 7621)
/// consumes from the inventory and triggers PcDeathService.Respawn.
/// First slice just routes the revive — the item consumption itself
/// runs through the existing ItemUseService when wired.
/// </summary>
public sealed class PlayerReviveItemService : IPlayerReviveItemService
{
    private readonly IPcDeathService _death;
    private readonly ILogger<PlayerReviveItemService> _logger;
    public PlayerReviveItemService(IPcDeathService death, ILogger<PlayerReviveItemService> logger)
    {
        _death = death;
        _logger = logger;
    }

    public bool TryRevive(PlayerEntity pc)
    {
        if (!_death.IsDead(pc)) return false;
        pc.Hp = pc.MaxHp;
        pc.Sp = pc.MaxSp;
        _death.Respawn(pc);
        _logger.LogInformation("pc_revive_item: char {Char} revived in place", pc.CharacterId);
        return true;
    }
}

/// <summary>Stub <see cref="IPlayerMacroDetectorService"/> — premium feature.</summary>
public sealed class PlayerMacroDetectorService : IPlayerMacroDetectorService
{
    public void DisconnectDetector(PlayerEntity pc) { }
    public void ProcessAnswer(PlayerEntity pc, string answer) { }
    public void CaptchaRegister(PlayerEntity pc) { }
    public void CaptchaRegisterUpload(PlayerEntity pc, byte[] image) { }
    public bool ReadCaptchaDb() => false;
    public void ReporterAreaSelect(PlayerEntity pc, short x1, short y1, short x2, short y2) { }
    public void ReporterProcess(PlayerEntity pc) { }
}
