using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// rAthena attendance: <c>pc_attendance_enabled</c>,
/// <c>pc_attendance_claim_reward</c> (pc.cpp:13740). Daily login
/// reward — the YAML schedule (db/re/attendance.yml) drives the
/// reward table. Stubbed until the schedule loader ports.
/// </summary>
public interface IPlayerAttendanceService
{
    bool IsEnabled();
    AttendanceClaimResult Claim(PlayerEntity pc, int day);
}

public enum AttendanceClaimResult
{
    Claimed,
    AlreadyClaimed,
    NotEnabled,
    OutOfRange,
}

/// <summary>
/// rAthena <c>pc_show_questinfo</c> / <c>pc_show_questinfo_reinit</c>
/// (pc.cpp:11920) — refresh the quest-marker bubbles above NPCs.
/// Stub until quest tracking + ZC_QUEST_NOTIFY_EFFECT batch port.
/// </summary>
public interface IPlayerQuestMarkerService
{
    /// <summary>Push current quest-marker state to <paramref name="pc"/>.</summary>
    void Refresh(PlayerEntity pc);

    /// <summary>Re-initialise (clear + repopulate).</summary>
    void Reinit(PlayerEntity pc);
}

/// <summary>
/// rAthena <c>pc_steal_item</c> (pc.cpp:8650) — Steal skill helper:
/// roll a stealable item off a target mob into the PC's inventory.
/// Stub until item-table marking (item flag stealable) ports.
/// </summary>
public interface IPlayerStealService
{
    /// <summary>Attempt a steal; returns true if an item was transferred.</summary>
    bool TrySteal(PlayerEntity thief, MobEntity target);
}

/// <summary>
/// rAthena <c>pc_reputation_generate</c> (pc.cpp:38) — populate the
/// reputation list (faction standings) at session enter. First slice
/// no-op until reputation_db.yml ports.
/// </summary>
public interface IPlayerReputationService
{
    void Generate(PlayerEntity pc);
}

/// <summary>rAthena <c>pc_show_version</c> — diagnostic overlay for the running build.</summary>
public interface IPlayerVersionDisplayService
{
    void Show(PlayerEntity pc);
}

/// <summary>rAthena <c>pc_revive_item</c> (pc.cpp:9710) — Token of Siegfried.</summary>
public interface IPlayerReviveItemService
{
    /// <summary>True if the item consumption + revive succeeded.</summary>
    bool TryRevive(PlayerEntity pc);
}

/// <summary>
/// rAthena macro-detector family (pc.cpp:14000-14400) —
/// <c>pc_macro_detector_*</c> + <c>pc_macro_captcha_*</c>. Premium
/// anti-bot subsystem; stubbed as a documented no-op so call sites
/// can wire without needing the full captcha pipeline.
/// </summary>
public interface IPlayerMacroDetectorService
{
    void DisconnectDetector(PlayerEntity pc);
    void ProcessAnswer(PlayerEntity pc, string answer);
    void CaptchaRegister(PlayerEntity pc);
    void CaptchaRegisterUpload(PlayerEntity pc, byte[] image);
    bool ReadCaptchaDb();
    void ReporterAreaSelect(PlayerEntity pc, short x1, short y1, short x2, short y2);
    void ReporterProcess(PlayerEntity pc);
}
