using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// rAthena player-event timer family: <c>pc_addeventtimer</c> /
/// <c>pc_deleventtimer</c> / <c>pc_cleareventtimer</c> /
/// <c>pc_addeventtimercount</c> (pc.cpp:7170-7290) — script-fired
/// callbacks bound to a player ("after 5s call OnEvent::Cleanup").
///
/// Bridges to the existing <c>Core.Timer</c> scheduler — each entry
/// is a one-shot timer whose payload is the script event name; the
/// scripting host dispatches when it fires.
/// </summary>
public interface IPlayerTimerService
{
    /// <summary>Add a one-shot event timer firing in <paramref name="ms"/>.</summary>
    int Add(PlayerEntity pc, string eventName, int ms);

    /// <summary>Cancel a timer by id (returned from <see cref="Add"/>).</summary>
    void Delete(PlayerEntity pc, int timerId);

    /// <summary>Cancel every timer attached to this PC.</summary>
    void ClearAll(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_addeventtimercount</c> — reschedule all of this
    /// PC's named-event timers by adding <paramref name="ms"/> to
    /// their fire tick (used by status-change pauses).
    /// </summary>
    int AddCountToAll(PlayerEntity pc, string eventName, int ms);
}

/// <summary>
/// rAthena battleground-queue timer wrappers: pc_set_bg_queue_timer /
/// pc_delete_bg_queue_timer (pc.cpp:7113-7150). Used while a PC waits
/// in a BG queue — the queue can expire if the player doesn't accept
/// the invitation within battleground_invitation_response.
/// </summary>
public interface IPlayerBgQueueTimerService
{
    /// <summary>Start a BG queue invitation timer.</summary>
    void Set(PlayerEntity pc, int ms);

    /// <summary>Cancel the queue invitation timer.</summary>
    void Delete(PlayerEntity pc);
}

/// <summary>
/// rAthena <c>pc_set_hate_mob</c> (pc.cpp:6890) — Taekwon "Feel"
/// / "Hate" map slots. Three positions; setting one overwrites.
/// </summary>
public interface IPlayerHateService
{
    /// <summary>Set the hated mob class for slot <paramref name="position"/> (0..2).</summary>
    bool SetHateMob(PlayerEntity pc, int position, int mobClassId);

    /// <summary>Look up the current hate target class id, or 0 if empty.</summary>
    int GetHateMob(PlayerEntity pc, int position);
}

/// <summary>
/// rAthena <c>pc_jail</c> / <c>pc_unjail</c> (atcommand.cpp:5825 +
/// pc.cpp:13165) — send the PC to the jail map for a duration.
/// </summary>
public interface IPlayerJailService
{
    /// <summary>Jail the PC for <paramref name="minutes"/> minutes (0 = indefinite).</summary>
    bool Jail(PlayerEntity pc, int minutes, string? reason = null);

    /// <summary>Release the PC.</summary>
    bool Unjail(PlayerEntity pc);

    /// <summary>True if the PC is currently jailed.</summary>
    bool IsJailed(PlayerEntity pc);
}
