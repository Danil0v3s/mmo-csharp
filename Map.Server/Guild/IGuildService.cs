using Core.Server.IPC;
using Map.Server.Entities;

namespace Map.Server.Guild;

/// <summary>
/// Top-level guild operations (creation / membership / GvG /
/// emblems / castle handoffs). Canonical entry points for rAthena
/// <c>guild.cpp</c> (2 755 lines, 74 public functions).
///
/// Guild data + persistence already live on the char server side
/// (CreateGuildAsync / GuildAddMemberAsync / etc.). The map-side
/// service here owns the in-world fan-out (chat broadcast, online
/// map-id sync) + the per-guild in-memory replica that the rest of
/// the gameplay code reads from (member iteration, alliance check,
/// permission gate).
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

    // -----------------------------------------------------------------
    // GD-H1 — in-memory replica + lookup surface
    // -----------------------------------------------------------------

    /// <summary>
    /// rAthena <c>guild_search</c> (cpp:166). Returns the in-memory
    /// <see cref="GuildEntity"/> if known to the map server, or null
    /// if no <see cref="OnRecvInfo"/> hydrate has landed yet.
    /// </summary>
    GuildEntity? Find(int guildId);

    /// <summary>
    /// rAthena <c>guild_recv_info</c> (cpp:822) — hydrate or refresh
    /// the in-memory replica from the char-side proto. Returns the
    /// updated entity. Idempotent; safe to call on every refresh.
    /// </summary>
    GuildEntity OnRecvInfo(GuildInfoData proto);

    /// <summary>Iterate all known guild entities. Used by xy-timer and reload helpers.</summary>
    System.Collections.Generic.IEnumerable<GuildEntity> All();

    /// <summary>Helper: count of cached guilds.</summary>
    int CachedCount { get; }

    // -----------------------------------------------------------------
    // GD-H2 — member tracking (joined / added / withdraw / memberinfoshort)
    // -----------------------------------------------------------------

    /// <summary>
    /// rAthena <c>guild_member_joined</c> (cpp:1073). Called from the
    /// session-enter path after PlayerEntity.GuildId is hydrated. If
    /// the guild is already cached, binds the PC to its member slot;
    /// otherwise dispatches a <c>guild_request_info</c> so the next
    /// recv-info hydrates the cache. Returns true when the bind hit
    /// the cached slot, false when info was requested instead.
    /// </summary>
    bool MemberJoined(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>guild_member_added</c> (cpp:1105). Inbound from the
    /// char server after an invite is accepted (flag=0) or rejected
    /// (flag=1). Updates <see cref="PlayerEntity.GuildId"/> on success
    /// and clears the pending-invite slot.
    /// </summary>
    int MemberAdded(int guildId, int accountId, int charId, int flag);

    /// <summary>
    /// rAthena <c>guild_member_withdraw</c> (cpp:1249). Inbound from
    /// the char server after a leave (flag=0) or expel (flag=1) is
    /// committed. Removes the cached member slot and clears
    /// <see cref="PlayerEntity.GuildId"/> on the affected PC. The
    /// <paramref name="name"/> / <paramref name="mes"/> pair drives
    /// the per-member-list "X has left / been expelled" notice.
    /// </summary>
    int MemberWithdraw(int guildId, int accountId, int charId, int flag, string name, string mes);

    /// <summary>
    /// rAthena <c>guild_send_memberinfoshort</c> (cpp:1363). Outbound
    /// trigger fired on level-up / job-change / move-map / login /
    /// logout. Updates the cached entry then dispatches the IPC so
    /// peer map servers see the same state. When <paramref name="online"/>
    /// is false we also drop the member's <c>sd</c> pointer (here we
    /// just flip the Online flag — the PlayerEntity binding is implicit
    /// via accountId/charId lookup).
    /// </summary>
    int SendMemberInfoShort(PlayerEntity pc, bool online);

    /// <summary>
    /// rAthena <c>guild_recv_memberinfoshort</c> (cpp:1397). Inbound
    /// short-form member status broadcast: updates online flag, level,
    /// class, recomputes ConnectMember + AverageLevel, and emits the
    /// member-login-notice broadcast (handled by clif side; here
    /// we record the change). Returns 1 when the update landed, 0
    /// when the guild or member wasn't found.
    /// </summary>
    int RecvMemberInfoShort(int guildId, int accountId, int charId, bool online, int lv, int classId);

    // -----------------------------------------------------------------
    // GD-H3 — permission gate
    // -----------------------------------------------------------------

    /// <summary>
    /// rAthena <c>guild_has_permission</c> (cpp:2640). Returns true
    /// when the PC's guild position has the given bit set. False
    /// when the PC isn't in a guild, the guild isn't cached, the PC
    /// isn't on the roster, or the position is missing/None.
    ///
    /// Master (position 0) implicitly has every permission; the
    /// hydrate path forces position 0's mode to
    /// <see cref="GuildPermission.All"/>.
    /// </summary>
    bool HasPermission(PlayerEntity pc, GuildPermission permission);

    // -----------------------------------------------------------------
    // GD-M1 — notice + emblem
    // -----------------------------------------------------------------

    /// <summary>
    /// rAthena <c>guild_change_notice</c> (cpp:1542). Outbound:
    /// caller PC must be in the named guild; we forward to the
    /// char server via <c>intif_guild_notice</c>. Returns true on
    /// gate pass.
    /// </summary>
    bool ChangeNotice(PlayerEntity pc, int guildId, string mes1, string mes2);

    /// <summary>
    /// rAthena <c>guild_notice_changed</c> (cpp:1553). Inbound from
    /// the char server after a notice mutation lands. Updates the
    /// cached entity then signals the per-member broadcast (clif
    /// side fires <c>clif_guild_notice</c>).
    /// </summary>
    int NoticeChanged(int guildId, string mes1, string mes2);

    /// <summary>
    /// rAthena <c>guild_check_emblem_change_condition</c> (cpp:1573).
    /// Returns true if the PC's guild is allowed to change emblem.
    /// Renewal+battle_config rule: require Glory of the Guild
    /// (GD_GLORYGUILD) skill if <c>require_glory_guild</c> is on.
    /// We default to permissive — the require-glory gate hooks in
    /// when the battle_config consumer ports.
    /// </summary>
    bool CheckEmblemChangeCondition(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>guild_change_emblem</c> (cpp:1587). Outbound:
    /// honors the change-condition gate then forwards the byte
    /// blob to the char server via <c>intif_guild_emblem</c>.
    /// </summary>
    int ChangeEmblem(PlayerEntity pc, byte[] data);

    /// <summary>
    /// rAthena <c>guild_change_emblem_version</c> (cpp:1598).
    /// PACKETVER ≥ 20200716 path — bumps the emblem version so
    /// clients pull a fresh blob.
    /// </summary>
    int ChangeEmblemVersion(PlayerEntity pc, int version);

    // -----------------------------------------------------------------
    // GD-M2 — alliance / opposition
    // -----------------------------------------------------------------

    /// <summary>
    /// rAthena <c>guild_reqalliance</c> (cpp:1853). Caller asks the
    /// target's guild for an alliance. Returns true if the gate passes
    /// (the actual prompt is fired by the wire layer). Gates:
    /// agit-not-running, target-in-different-guild, neither guild at
    /// MaxAlliance, target not pending another alliance.
    /// </summary>
    bool ReqAlliance(PlayerEntity sd, PlayerEntity tsd);

    /// <summary>
    /// rAthena <c>guild_reply_reqalliance</c> (cpp:1915). Target's
    /// response. flag=1 accept, flag=0 deny. Returns 1 on dispatch.
    /// </summary>
    int ReplyReqAlliance(PlayerEntity sd, int requesterAccountId, int flag);

    /// <summary>
    /// rAthena <c>guild_delalliance</c> (cpp:1974). Breaks an existing
    /// relation (flag=0 ally, flag=1 enemy). Blocked while agit
    /// (WoE) is running.
    /// </summary>
    int DelAlliance(PlayerEntity sd, int otherGuildId, int flag);

    /// <summary>
    /// rAthena <c>guild_opposition</c> (cpp:1989). Declare enemy.
    /// Gates: not-same-guild, not at MaxAlliance enemies, not already
    /// enemy.
    /// </summary>
    int Opposition(PlayerEntity sd, PlayerEntity tsd);

    /// <summary>
    /// rAthena <c>guild_allianceack</c> (cpp:2030). Inbound from the
    /// char server after a relation mutation lands. Flag encoding:
    /// <list type="bullet">
    /// <item><c>flag &amp; 0x70</c> != 0: failure (no mutation).</item>
    /// <item><c>flag &amp; 0x08</c> == 0: create relation.</item>
    /// <item><c>flag &amp; 0x08</c> != 0: remove relation.</item>
    /// <item><c>flag &amp; 0x01</c>: 0 = ally, 1 = enemy.</item>
    /// </list>
    /// Mutates both sides' <see cref="GuildEntity.Alliances"/>.
    /// Returns 1 on mutation, 0 on failure.
    /// </summary>
    int OnAllianceAck(int guildId1, int guildId2, string name1, string name2, int flag);

    // -----------------------------------------------------------------
    // GD-L1 — misc helpers + skill table
    // -----------------------------------------------------------------

    /// <summary>rAthena <c>guild_checkskill</c> (cpp:246). Returns the guild's learned skill level or 0.</summary>
    int CheckSkill(int guildId, ushort skillId);

    /// <summary>rAthena <c>guild_skill_get_max</c> (cpp:235). Per-skill max-level lookup (GD_*).</summary>
    ushort SkillGetMax(ushort skillId);

    /// <summary>rAthena <c>guild_check_skill_require</c> (cpp:255). True if all prereqs are met.</summary>
    bool CheckSkillRequire(int guildId, ushort skillId);

    /// <summary>rAthena <c>guild_block_skill</c> (cpp:1825). Registers a per-PC cooldown for the common guild skills (Battle Orders / Regen / Restore / Emergency Call).</summary>
    void BlockSkill(PlayerEntity pc, int durationMs);

    /// <summary>Returns the remaining cooldown for the given guild skill on the given PC, or 0 if none.</summary>
    int GetBlockedSkillRemaining(PlayerEntity pc, ushort skillId);

    /// <summary>rAthena <c>guild_skillupack</c> (cpp:1754). Inbound: mutate cached skill table.</summary>
    int SkillUpAck(int guildId, ushort skillId, int accountId);

    /// <summary>rAthena <c>guild_guildaura_refresh</c> (cpp:1786). Reapply guild-aura SC on each online member. Marker only here — the SC apply lands when the status consumer ports.</summary>
    void GuildAuraRefresh(PlayerEntity caster, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>guild_getavailablesd</c> (cpp:576). Returns the first online member's CharId, or 0 if none.</summary>
    int GetAvailableMemberCharId(int guildId);

    /// <summary>
    /// rAthena <c>guild_retrieveitembound</c> (cpp:1321). When a member
    /// is expelled with bound items, char-side mails them back. The
    /// map-side trigger fires here; the actual fetch is char-side
    /// (BOUND_ITEMS path).
    /// </summary>
    int RetrieveItemBound(int charId, int accountId, int guildId);

    /// <summary>rAthena <c>guild_broken_sub</c> (cpp:2114). When guild X is disbanded, clear references to it from every other cached guild's alliance table.</summary>
    int BrokenSub(int brokenGuildId);

    /// <summary>rAthena <c>guild_send_xy_timer_sub</c> (cpp:656). For each cached guild, return the online member CharIds. The wire side iterates these to emit <c>clif_guild_xy</c>.</summary>
    System.Collections.Generic.IReadOnlyList<int> SendXyTimerSub(int guildId);

    /// <summary>rAthena <c>guild_flag_add</c> (cpp:2650). Register a guild-flag NPC.</summary>
    void FlagAdd(int npcId);
    /// <summary>rAthena <c>guild_flag_remove</c> (cpp:2667).</summary>
    void FlagRemove(int npcId);
    /// <summary>rAthena <c>guild_flags_clear</c> (cpp:2715).</summary>
    void FlagsClear();
    /// <summary>Snapshot of currently registered guild-flag NPC ids.</summary>
    System.Collections.Generic.IReadOnlyList<int> GetFlagNpcs();

    // -----------------------------------------------------------------
    // GD-L2 — GM transfer + ack handlers + request-info + broken
    // -----------------------------------------------------------------

    /// <summary>
    /// rAthena <c>guild_created</c> (cpp:722). Inbound from char
    /// server after create. guildId=0 means the create failed
    /// (duplicate name). On success, sets <see cref="PlayerEntity.GuildId"/>
    /// on the master.
    /// </summary>
    int OnGuildCreated(PlayerEntity master, int guildId);

    /// <summary>rAthena <c>guild_request_info</c> (cpp:745). Outbound — pull a full GuildInfoResponse.</summary>
    int RequestInfo(int guildId);

    /// <summary>rAthena <c>guild_npc_request_info</c> (cpp:750). Outbound, optionally bound to an NPC event name. Returns 1 when cached, 0 when a request was dispatched.</summary>
    int NpcRequestInfo(int guildId, string npcEventName);

    /// <summary>rAthena <c>guild_position_changed</c> (cpp:1524). Inbound — paint cached position table.</summary>
    int OnPositionChanged(int guildId, int idx, GuildPermission mode, int expMode, string name);

    /// <summary>rAthena <c>guild_memberposition_changed</c> (cpp:1497). Inbound — flip a member's position index.</summary>
    int OnMemberPositionChanged(int guildId, int idx, int newPosition);

    /// <summary>rAthena <c>guild_broken</c> (cpp:2149). Inbound — purge the guild (delegates to BrokenSub) plus signal disband to all members.</summary>
    int OnBroken(int guildId, int flag);

    /// <summary>rAthena <c>guild_gm_change</c> (cpp:2193). Outbound — request master transfer to the given char.</summary>
    bool GmChange(int guildId, int charId);

    /// <summary>rAthena <c>guild_gm_changed</c> (cpp:2229). Inbound — swaps Members[0] with Members[pos]; updates MasterCharId / MasterName.</summary>
    int OnGmChanged(int guildId, int accountId, int charId, long timestamp);
}
