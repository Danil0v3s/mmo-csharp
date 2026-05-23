using Map.Server.Entities;

namespace Map.Server.BattleGround;

/// <summary>
/// Battleground queue + team management. Canonical entry points for
/// rAthena <c>battleground.cpp</c> (1 617 lines, 29 functions).
/// </summary>
public interface IBattlegroundService
{
    /// <summary>rAthena <c>bg_create</c>.</summary>
    int Create(int mapIndex, byte side);

    /// <summary>rAthena <c>bg_team_join</c>.</summary>
    bool TeamJoin(int bgId, PlayerEntity pc);

    /// <summary>rAthena <c>bg_team_leave</c>.</summary>
    int TeamLeave(PlayerEntity pc, byte flag);

    /// <summary>rAthena <c>bg_team_delete</c>.</summary>
    bool TeamDelete(int bgId);

    /// <summary>rAthena <c>bg_team_warp</c>.</summary>
    bool TeamWarp(int bgId, int mapIndex, short x, short y);

    /// <summary>rAthena <c>bg_team_get_id</c>.</summary>
    int TeamGetId(PlayerEntity pc);

    /// <summary>rAthena <c>bg_member_respawn</c>.</summary>
    bool MemberRespawn(PlayerEntity pc);

    /// <summary>rAthena <c>bg_player_is_in_bg_map</c>.</summary>
    bool PlayerIsInBgMap(PlayerEntity pc);

    /// <summary>rAthena <c>bg_mapflag_check</c>.</summary>
    bool MapflagCheck(PlayerEntity pc, ushort skillId);

    /// <summary>rAthena <c>bg_getavailablesd</c>.</summary>
    PlayerEntity? GetAvailableSd(int bgId);

    /// <summary>rAthena <c>bg_queue_check_joinable</c>.</summary>
    bool QueueCheckJoinable(PlayerEntity pc, int queueId);

    /// <summary>rAthena <c>bg_queue_leave</c>.</summary>
    bool QueueLeave(PlayerEntity pc);

    /// <summary>rAthena <c>bg_queue_on_ready</c>.</summary>
    bool QueueOnReady(int queueId);

    /// <summary>rAthena <c>bg_queue_reservation</c>.</summary>
    bool QueueReservation(int queueId);

    /// <summary>rAthena <c>bg_queue_clear</c>.</summary>
    void QueueClear(int queueId);

    /// <summary>rAthena <c>bg_queue_join_solo</c>.</summary>
    void QueueJoinSolo(PlayerEntity pc, string queueName);

    /// <summary>rAthena <c>bg_queue_join_party</c>.</summary>
    void QueueJoinParty(PlayerEntity leader, string queueName);

    /// <summary>rAthena <c>bg_queue_join_guild</c>.</summary>
    void QueueJoinGuild(PlayerEntity leader, string queueName);

    /// <summary>rAthena <c>bg_queue_join_multi</c>.</summary>
    void QueueJoinMulti(PlayerEntity leader, string queueName, byte mode);

    /// <summary>rAthena <c>bg_queue_on_accept_invite</c>.</summary>
    void QueueOnAcceptInvite(PlayerEntity pc, int queueId);

    /// <summary>rAthena <c>bg_queue_start_battleground</c>.</summary>
    void QueueStartBattleground(int queueId);

    /// <summary>rAthena <c>bg_join_active</c>.</summary>
    void JoinActive(PlayerEntity pc);

    /// <summary>rAthena <c>bg_send_xy_timer_sub</c>.</summary>
    void SendXyTimerSub(int bgId);

    /// <summary>rAthena <c>bg_send_dot_remove</c>.</summary>
    void SendDotRemove(PlayerEntity pc);

    /// <summary>rAthena <c>bg_send_message</c>.</summary>
    void SendMessage(int bgId, string sender, string text);

    /// <summary>Snapshot a named queue (returns null if not registered) — backs <c>@bg &lt;name&gt;</c>.</summary>
    (int QueueId, string Name, BgQueueState State, int Members, byte Required)? GetQueueSnapshot(string queueName);

    /// <summary>List every registered queue — backs <c>@bg</c> with no args.</summary>
    IReadOnlyList<(int QueueId, string Name, BgQueueState State, int Members, byte Required)> GetAllQueues();

    /// <summary>End a queue immediately (rAthena <c>bg_queue_clear(true)</c>) — backs <c>@bgend</c>.</summary>
    void QueueEnd(int queueId);

    /// <summary>Mark a player as queue leader — backs <c>@bgleader</c>.</summary>
    bool SetQueueLeader(PlayerEntity pc);
}
