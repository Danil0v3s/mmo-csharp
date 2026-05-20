using Map.Server.Entities;

namespace Map.Server.Clan;

/// <summary>
/// Clan membership + chat helpers. Canonical entry points for
/// rAthena <c>clan.cpp</c> (235 lines, 13 public functions).
///
/// Clans differ from guilds: pre-built rosters (clan_db.yml), no
/// player creation, no castle ownership. The Char server already
/// loads <c>clan_db.yml</c> + tracks `clan_inactive_kick_days`;
/// the map-side service here handles join / leave + per-clan chat
/// fan-out.
/// </summary>
public interface IClanService
{
    /// <summary>rAthena <c>clan_member_join</c> — join after the join packet validates.</summary>
    bool MemberJoin(PlayerEntity pc, int clanId);

    /// <summary>rAthena <c>clan_member_leave</c>.</summary>
    bool MemberLeave(PlayerEntity pc);

    /// <summary>rAthena <c>clan_member_joined</c> — fan-out join broadcast.</summary>
    void MemberJoined(PlayerEntity pc);

    /// <summary>rAthena <c>clan_member_left</c> — fan-out leave broadcast.</summary>
    void MemberLeft(PlayerEntity pc);

    /// <summary>rAthena <c>clan_load_clandata</c> — hydrate the clan roster onto the PC.</summary>
    void LoadClanData(PlayerEntity pc);

    /// <summary>rAthena <c>clan_getMemberIndex</c>.</summary>
    int GetMemberIndex(int clanId, PlayerEntity pc);

    /// <summary>rAthena <c>clan_getNextFreeMemberIndex</c>.</summary>
    int GetNextFreeMemberIndex(int clanId);

    /// <summary>rAthena <c>clan_get_alliance_count</c>.</summary>
    int GetAllianceCount(int clanId);

    /// <summary>rAthena <c>clan_getavailablesd</c> — find any online member.</summary>
    PlayerEntity? GetAvailableMember(int clanId);

    /// <summary>rAthena <c>clan_send_message</c> — broadcast a clan chat line.</summary>
    void SendMessage(PlayerEntity from, string text);

    /// <summary>rAthena <c>clan_recv_message</c> — receive a chat line forwarded from another map server.</summary>
    void RecvMessage(int clanId, string sender, string text);
}
