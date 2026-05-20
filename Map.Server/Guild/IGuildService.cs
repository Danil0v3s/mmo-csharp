using Map.Server.Entities;

namespace Map.Server.Guild;

/// <summary>
/// Top-level guild operations (creation / membership / GvG /
/// emblems / castle handoffs). Canonical entry points for rAthena
/// <c>guild.cpp</c> (2 755 lines, 79 public functions).
///
/// Guild data + persistence already live on the char server side
/// (CreateGuildAsync / GuildAddMemberAsync / etc.). The map-side
/// service here owns the in-world fan-out (chat broadcast, online
/// map-id sync) + per-guild emergency state.
/// </summary>
public interface IGuildService
{
    /// <summary>rAthena <c>guild_create</c>.</summary>
    int Create(PlayerEntity master, string name);
    /// <summary>rAthena <c>guild_invite</c>.</summary>
    bool Invite(PlayerEntity inviter, PlayerEntity invitee);
    /// <summary>rAthena <c>guild_reply_invite</c>.</summary>
    bool ReplyInvite(PlayerEntity invitee, int guildId, byte ok);
    /// <summary>rAthena <c>guild_leave</c>.</summary>
    bool Leave(PlayerEntity pc, int guildId, int accountId, int charId, string reason);
    /// <summary>rAthena <c>guild_expulsion</c>.</summary>
    bool Expulsion(PlayerEntity gm, int guildId, int accountId, int charId, string reason);
    /// <summary>rAthena <c>guild_send_message</c>.</summary>
    int SendMessage(PlayerEntity from, string text);
    /// <summary>rAthena <c>guild_recv_message</c>.</summary>
    int RecvMessage(int guildId, string sender, string text);
    /// <summary>rAthena <c>guild_change_position</c>.</summary>
    bool ChangePosition(PlayerEntity gm, int idx, int mode, int exp_mode, string name);
    /// <summary>rAthena <c>guild_change_memberposition</c>.</summary>
    int ChangeMemberPosition(int guildId, int accountId, int charId, short idx);
    /// <summary>rAthena <c>guild_emblem_changed</c>.</summary>
    int EmblemChanged(int guildId);
    /// <summary>rAthena <c>guild_skillup</c>.</summary>
    bool SkillUp(PlayerEntity pc, ushort skillId);
    /// <summary>rAthena <c>guild_allianceack</c>.</summary>
    int AllianceAck(int guildId, int allyId, int accountId, int charId, int flag, string mes);
    /// <summary>rAthena <c>guild_break</c>.</summary>
    bool Break(PlayerEntity gm, string name);
    /// <summary>rAthena <c>guild_castledataload</c>.</summary>
    int CastleDataLoad();
    /// <summary>rAthena <c>guild_castledataloadack</c>.</summary>
    int CastleDataLoadAck(int castleId, int index, int value);
    /// <summary>rAthena <c>guild_castledatasave</c>.</summary>
    int CastleDataSave(int castleId, int index, int value);
    /// <summary>rAthena <c>guild_check_alliance</c>.</summary>
    int CheckAlliance(int guild1, int guild2, byte flag);
    /// <summary>rAthena <c>guild_check_member</c>.</summary>
    bool CheckMember(int guildId, PlayerEntity pc);
    /// <summary>rAthena <c>guild_addmember</c>.</summary>
    int AddMember(int guildId, PlayerEntity pc);
    /// <summary>rAthena <c>guild_send_xy_timer</c>.</summary>
    int SendXyTimer(int guildId);
    /// <summary>rAthena <c>guild_send_dot_remove</c>.</summary>
    int SendDotRemove(PlayerEntity pc);
    /// <summary>rAthena <c>guild_recv_info</c>.</summary>
    int RecvInfo(int guildId);
    /// <summary>rAthena <c>guild_recv_noinfo</c>.</summary>
    int RecvNoInfo(int guildId);
    /// <summary>rAthena <c>guild_send_levelup</c>.</summary>
    void SendLevelUp(PlayerEntity pc);
    /// <summary>rAthena <c>guild_reload</c>.</summary>
    void Reload();
    /// <summary>rAthena <c>do_init_guild</c>.</summary>
    void Init();
    /// <summary>rAthena <c>do_final_guild</c>.</summary>
    void Final();
}
