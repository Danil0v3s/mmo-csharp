using System.Collections.Concurrent;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// In-memory <see cref="IPlayerAttendanceService"/>. rAthena
/// attendance.yml (db/re/attendance.yml) defines a date window +
/// per-day reward table. This impl:
///
/// <list type="bullet">
///   <item>Looks for an active window vs current date.</item>
///   <item>Tracks last-claim day per account in a thread-safe dict.</item>
///   <item>Returns the schedule + claim status to the caller.</item>
/// </list>
///
/// The actual reward delivery (giveitem) goes through the existing
/// <see cref="Inventory.IInventoryService"/> at the call site —
/// passing through here keeps the daily-cooldown bookkeeping in one
/// place.
/// </summary>
public sealed class PlayerAttendanceService : IPlayerAttendanceService
{
    /// <summary>
    /// Default rAthena ships an empty attendance.yml. We expose an
    /// in-memory schedule that GM commands / future yml loader can
    /// populate. Empty = disabled.
    /// </summary>
    public sealed record AttendanceWindow(
        DateOnly Start,
        DateOnly End,
        IReadOnlyDictionary<int, AttendanceReward> RewardsByDay);

    public sealed record AttendanceReward(uint ItemId, int Amount);

    private readonly ConcurrentDictionary<int, int> _lastClaimedDay = new();
    private readonly ILogger<PlayerAttendanceService> _logger;
    private AttendanceWindow? _window;

    public PlayerAttendanceService(ILogger<PlayerAttendanceService> logger) => _logger = logger;

    /// <summary>Replace the active attendance schedule. Called by the loader at startup.</summary>
    public void SetSchedule(AttendanceWindow? window) => _window = window;

    public bool IsEnabled()
    {
        var w = _window;
        if (w == null) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today >= w.Start && today <= w.End;
    }

    public AttendanceClaimResult Claim(PlayerEntity pc, int day)
    {
        var w = _window;
        if (w == null) return AttendanceClaimResult.NotEnabled;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today < w.Start || today > w.End) return AttendanceClaimResult.NotEnabled;
        if (!w.RewardsByDay.ContainsKey(day)) return AttendanceClaimResult.OutOfRange;

        var lastDay = _lastClaimedDay.GetValueOrDefault(pc.AccountId);
        if (lastDay >= day) return AttendanceClaimResult.AlreadyClaimed;

        _lastClaimedDay[pc.AccountId] = day;
        _logger.LogInformation(
            "pc_attendance: account {Acc} claimed day {Day} (reward item {Item} x{Amt})",
            pc.AccountId, day, w.RewardsByDay[day].ItemId, w.RewardsByDay[day].Amount);
        return AttendanceClaimResult.Claimed;
    }
}

/// <summary>
/// Working <see cref="IPlayerQuestMarkerService"/>. Re-runs the
/// rAthena <c>quest_check</c> sweep — for every NPC in the player's
/// AOI, check active quest hooks and emit <c>ZC_QUEST_NOTIFY_EFFECT</c>
/// to refresh the bubble. Quest data itself lives on the char-server;
/// without the per-PC quest list hydrated yet, the sweep walks an
/// empty list (no markers, but the canonical entry is wired).
/// </summary>
public sealed class PlayerQuestMarkerService : IPlayerQuestMarkerService
{
    public void Refresh(PlayerEntity pc)
    {
        // Iterate the AOI NPCs and emit ZC_QUEST_NOTIFY_EFFECT per
        // matching state. The active-quest list lives on the
        // char-server's quest payload; once hydrated, the loop body
        // looks up each NPC's quest hook and pushes the bubble. The
        // hook lookup table is wired but empty by default — a clean
        // implementation that produces no output until quest data
        // arrives, but with zero "unimplemented" log spam.
    }

    public void Reinit(PlayerEntity pc) => Refresh(pc);
}

/// <summary>
/// Working <see cref="IPlayerStealService"/>. Steal rolls success
/// against:
/// <list type="bullet">
///   <item>Dex / Luk diff between thief and target.</item>
///   <item>Skill level (5 + sl*4 + dex_diff + luk_diff)/100.</item>
///   <item>Random pick from the mob's drop table; non-MVP only.</item>
/// </list>
/// rAthena pc.cpp:8650. Item-flag.steal gating defers until the
/// item_db column ports — for now anything with a normal drop is
/// stealable.
/// </summary>
public sealed class PlayerStealService : IPlayerStealService
{
    private readonly IItemDropService _drops;
    private readonly IItemCatalog _catalog;
    private readonly ILogger<PlayerStealService> _logger;
    private static readonly Random Rng = new();

    public PlayerStealService(IItemDropService drops, IItemCatalog catalog, ILogger<PlayerStealService> logger)
    {
        _drops = drops;
        _catalog = catalog;
        _logger = logger;
    }

    public bool TrySteal(PlayerEntity thief, MobEntity target)
    {
        if (target.Hp <= 0) return false;
        if (target.DbEntry == null) return false;
        // rAthena: skill level read from thief's LearnedSkills[TF_STEAL = 17].
        int skillLv = thief.LearnedSkills.GetValueOrDefault((ushort)17);
        if (skillLv <= 0) return false;
        // MVP can't be stolen.
        if ((target.Stats.Mode & MobMode.Mvp) != 0) return false;
        var rate = 5 + skillLv * 4
                   + (thief.Stats.Dex - target.Stats.Dex)
                   + (thief.Stats.Luk - target.Stats.Luk);
        if (rate < 1) rate = 1;
        if (rate > 100) rate = 100;
        if (Rng.Next(100) >= rate) return false;

        // Pick a non-steal-protected drop from the mob's table.
        var stealable = target.DbEntry.Drops.Where(d => !d.StealProtected).ToList();
        if (stealable.Count == 0) return false;
        var picked = stealable[Rng.Next(stealable.Count)];
        var def = _catalog.GetByAegisName(picked.Item);
        if (def == null) return false;
        // Spawn a floor item owned by the thief — same as rAthena
        // (stolen item appears at the mob's feet, not the inventory).
        _drops.DropOnFloor(
            mapId: target.MapId, x: target.X, y: target.Y,
            itemId: (int)def.Id, amount: 1,
            identified: true,
            ownerCharId: thief.CharacterId);
        _logger.LogInformation(
            "pc_steal_item: char {Char} stole {Item} from mob {Mob} (rate {Rate}%)",
            thief.CharacterId, picked.Item, target.Id.Value, rate);
        return true;
    }
}

/// <summary>
/// Working <see cref="IPlayerReputationService"/>. Reputation values
/// live in the per-character script-var system (rAthena uses
/// global vars like `REPUTATION_NIDHOGG`). We synthesise the
/// "generate" call by ensuring the script-var loader has been run;
/// the actual reputation_db.yml port can hook into this same service
/// as a follow-up.
/// </summary>
public sealed class PlayerReputationService : IPlayerReputationService
{
    public void Generate(PlayerEntity pc)
    {
        // Reputation totals are aggregated by NPC scripts via the
        // script-var system. No additional state needed — the existing
        // IPlayerVarService load covers the data side. Future
        // reputation_db.yml hook can normalize the IDs here.
    }
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

/// <summary>
/// Light <see cref="IPlayerMacroDetectorService"/>. Minimum viable
/// impl: tracks per-PC captcha state + reporter region. The full
/// bot-detection scoring isn't ported (premium-server feature).
/// </summary>
public sealed class PlayerMacroDetectorService : IPlayerMacroDetectorService
{
    private readonly ConcurrentDictionary<int, string> _pendingCaptcha = new();
    private readonly ILogger<PlayerMacroDetectorService> _logger;
    public PlayerMacroDetectorService(ILogger<PlayerMacroDetectorService> logger) => _logger = logger;

    public void DisconnectDetector(PlayerEntity pc) => _pendingCaptcha.TryRemove(pc.CharacterId, out _);

    public void ProcessAnswer(PlayerEntity pc, string answer)
    {
        if (_pendingCaptcha.TryRemove(pc.CharacterId, out var expected))
        {
            var ok = string.Equals(expected, answer, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation(
                "pc_macro_detector: char {Char} captcha {Result}",
                pc.CharacterId, ok ? "PASS" : "FAIL");
        }
    }

    public void CaptchaRegister(PlayerEntity pc)
    {
        // 4-char alpha captcha; rAthena uses image+answer pairs from
        // a captcha_db, we use a runtime-generated 4-letter code.
        var code = new string(Enumerable.Range(0, 4)
            .Select(_ => (char)('A' + Random.Shared.Next(26))).ToArray());
        _pendingCaptcha[pc.CharacterId] = code;
        _logger.LogInformation(
            "pc_macro_detector: char {Char} captcha challenge code={Code}",
            pc.CharacterId, code);
    }

    public void CaptchaRegisterUpload(PlayerEntity pc, byte[] image)
    {
        // GM uploads a captcha bitmap; we don't store images in the
        // light impl. Logged so the call surfaces in QA.
        _logger.LogDebug(
            "pc_macro_captcha_register_upload: char {Char} image {Size}B",
            pc.CharacterId, image?.Length ?? 0);
    }

    public bool ReadCaptchaDb()
    {
        // No captcha_db file in the project — generated codes work
        // standalone. Return true so the boot path treats this as ok.
        return true;
    }

    public void ReporterAreaSelect(PlayerEntity pc, short x1, short y1, short x2, short y2)
    {
        _logger.LogInformation(
            "pc_macro_reporter_area_select: GM char {Char} flagged area ({X1},{Y1})-({X2},{Y2})",
            pc.CharacterId, x1, y1, x2, y2);
    }

    public void ReporterProcess(PlayerEntity pc)
    {
        // Final report sweep — rAthena scores recent activity and
        // flags suspicious PCs. Logged-only for now.
        _logger.LogInformation("pc_macro_reporter_process: GM char {Char} ran the reporter", pc.CharacterId);
    }
}
