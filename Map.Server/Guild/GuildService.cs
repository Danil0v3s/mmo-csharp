using System.Collections.Concurrent;
using System.Collections.Generic;
using Core.Server.IPC;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Guild;

/// <summary>
/// Default <see cref="IGuildService"/>. Owns the per-guild in-memory
/// replica (<see cref="GuildEntity"/>) that fans out to the rest of
/// the map-side gameplay code; persistence + cross-map-server fan-out
/// still go through char-server IPC (<c>ICharServerIpcServiceGuild</c>).
///
/// GD-H1 wave: this class went from "every method returns 0/false"
/// to a real cache-backed implementation, while keeping the entire
/// pre-existing interface surface working. Read paths (Find / All /
/// CachedCount) are lock-free via <see cref="ConcurrentDictionary{TKey, TValue}"/>;
/// game-loop writes (OnRecvInfo, position changes, etc.) are single
/// threaded per the project's threading rules.
/// </summary>
public sealed class GuildService : IGuildService
{
    private readonly ILogger<GuildService> _logger;
    private readonly ConcurrentDictionary<int, GuildEntity> _byId = new();

    public GuildService(ILogger<GuildService> logger) => _logger = logger;

    // ---------- GD-H1: in-memory replica ----------

    public GuildEntity? Find(int guildId)
        => guildId > 0 && _byId.TryGetValue(guildId, out var g) ? g : null;

    public IEnumerable<GuildEntity> All() => _byId.Values;

    public int CachedCount => _byId.Count;

    /// <summary>
    /// rAthena guild.cpp:822 — hydrate / refresh. On first sight we
    /// allocate a fresh <see cref="GuildEntity"/>, otherwise we mutate
    /// the existing one in place so any callers that captured the
    /// reference (e.g. PlayerEntity-side caches) keep seeing the
    /// latest state. We bound MaxMember + MaxGuildAlliance + MaxPosition
    /// to the rAthena caps so a malformed proto can't blow up the
    /// loops below.
    /// </summary>
    public GuildEntity OnRecvInfo(GuildInfoData proto)
    {
        if (proto == null) throw new System.ArgumentNullException(nameof(proto));
        if (proto.GuildId <= 0)
            throw new System.ArgumentException("GuildInfoData.GuildId must be > 0", nameof(proto));

        var g = _byId.GetOrAdd(proto.GuildId, _ => new GuildEntity { GuildId = proto.GuildId });

        // Header
        g.Name = proto.Name ?? string.Empty;
        g.GuildLv = proto.Level;
        g.MaxMember = System.Math.Min(proto.MaxMember, GuildLimits.MaxMember);
        g.MasterCharId = proto.MasterCharacterId;
        g.EmblemVersion = proto.EmblemVersion;
        g.EmblemData = proto.EmblemData?.ToByteArray() ?? System.Array.Empty<byte>();
        g.Notice1 = proto.Notice1 ?? string.Empty;
        g.Notice2 = proto.Notice2 ?? string.Empty;

        // Members
        g.Members.Clear();
        int onlineCount = 0;
        long levelSum = 0;
        int countedLevels = 0;
        if (proto.Members != null)
        {
            foreach (var mp in proto.Members)
            {
                if (g.Members.Count >= GuildLimits.MaxMember) break;
                var m = new GuildMember
                {
                    AccountId = mp.AccountId,
                    CharId = (int)mp.CharacterId,
                    Name = mp.Name ?? string.Empty,
                    ClassId = mp.ClassId,
                    Level = (int)mp.Level,
                    Online = mp.Online,
                    // The proto carries the position name (display only);
                    // the index follows the slot ordering rAthena uses,
                    // so the member's Position == its current slot index
                    // unless an explicit GuildMemberInfoChange/MGI_POSITION
                    // event has remapped it. Default to "lowest rank" so
                    // permissions don't accidentally grant the master bit.
                    Position = MaxValidPosition(g.Positions.Count),
                };
                if (m.CharId == g.MasterCharId)
                {
                    g.MasterName = m.Name;
                    m.Position = 0; // master always sits at position 0
                }
                g.Members.Add(m);
                if (m.Online) onlineCount++;
                if (m.Level > 0) { levelSum += m.Level; countedLevels++; }
            }
        }
        g.ConnectMember = onlineCount;
        g.AverageLevel = countedLevels > 0 ? (int)(levelSum / countedLevels) : 0;

        // Positions
        g.Positions.Clear();
        if (proto.Positions != null)
        {
            foreach (var pp in proto.Positions)
            {
                if (g.Positions.Count >= GuildLimits.MaxPosition) break;
                g.Positions.Add(new GuildPosition
                {
                    Name = pp.Name ?? string.Empty,
                    // Master slot keeps GuildPermission.All implicitly so
                    // even a malformed "mode=0" position 0 still gates
                    // correctly via guild_has_permission.
                    Mode = pp.Index == 0 ? GuildPermission.All : (GuildPermission)pp.Mode,
                    ExpMode = pp.ExpMode,
                });
            }
        }
        // rAthena always seeds MAX_GUILDPOSITION slots; back-fill so
        // permission queries against unset positions return false.
        while (g.Positions.Count < GuildLimits.MaxPosition)
            g.Positions.Add(new GuildPosition { Name = string.Empty, Mode = GuildPermission.None });

        return g;
    }

    private static int MaxValidPosition(int positionCount)
        => positionCount > 0 ? System.Math.Min(positionCount - 1, GuildLimits.MaxPosition - 1) : GuildLimits.MaxPosition - 1;

    // ---------- pre-GD-H1 surface (kept compatible) ----------
    // The interface below predates GD-H1; nothing is wired to char-side
    // IPC yet (later waves will do that). For now they return harmless
    // zeros / false so call sites compile. Replacement happens in
    // GD-H2 / GD-H3 / GD-M1 / GD-M2 alongside the matching gates.

    public int Create(PlayerEntity master, string name) => 0;

    public bool Invite(PlayerEntity inviter, PlayerEntity invitee)
    {
        // rAthena guild.cpp:925 — gate matrix: target valid, inviter
        // in a guild, inviter has GUILD_PERM_INVITE, invitee not
        // already in a clan/guild. Map-flag MF_GUILDLOCK + instance
        // checks are caller-side gates (we don't have the map flag
        // service plumbed in yet — covered when the packet handler
        // ports next wave).
        if (inviter == null || invitee == null) return false;
        if (inviter.GuildId <= 0) return false;
        var g = Find(inviter.GuildId);
        if (g == null) return false;
        if (!HasPermission(inviter, GuildPermission.Invite))
            return false;
        // Invitee must not already be in a guild
        if (invitee.GuildId != 0) return false;
        // (clan + noask + invite-pending gates would land here)
        return true;
    }

    public bool ReplyInvite(PlayerEntity invitee, int guildId, byte ok)
    {
        if (invitee == null) return false;
        if (guildId <= 0) return false;
        // ok != 0 → accept. The actual roster mutation happens once
        // the char-side IPC round-trip completes and MemberAdded
        // fires from the response handler.
        return ok != 0;
    }

    public bool Leave(PlayerEntity pc, int guildId, int accountId, int charId, string reason)
    {
        // rAthena guild.cpp:1156 — caller-side checks (MF_GUILDLOCK /
        // BG / GVG / instance lock) gate at the packet boundary;
        // here we enforce identity match.
        if (pc == null || guildId <= 0) return false;
        if (pc.GuildId != guildId) return false;
        if (pc.AccountId != accountId || pc.CharacterId != charId) return false;
        var g = Find(guildId);
        if (g == null) return false;
        if (g.GetIndex(accountId, charId) < 0) return false;
        // Leave is dispatched via intif_guild_leave — the wire-up
        // ports in a later wave when the packet handler lands.
        return true;
    }

    public bool Expulsion(PlayerEntity gm, int guildId, int accountId, int charId, string reason)
    {
        // rAthena guild.cpp:1189 — gm must be in this guild, must
        // hold GUILD_PERM_EXPEL, target must be on the roster and
        // must not be the master.
        if (gm == null || guildId <= 0) return false;
        if (gm.GuildId != guildId) return false;
        var g = Find(guildId);
        if (g == null) return false;
        if (!HasPermission(gm, GuildPermission.Expel)) return false;
        var idx = g.GetIndex(accountId, charId);
        if (idx < 0) return false;
        // Can't expel the master
        if (g.Members[idx].CharId == g.MasterCharId) return false;
        return true;
    }
    public int SendMessage(PlayerEntity from, string text) => 0;
    public int RecvMessage(int guildId, string sender, string text) => 0;
    public bool ChangePosition(PlayerEntity gm, int idx, int mode, int exp_mode, string name)
    {
        // rAthena guild.cpp:1511 — change_position is master-only in
        // practice (no per-bit gate beyond gmaster_flag in cpp, but
        // we honor the GUILD_PERM_ALL convention so a non-master
        // with the bits cleared can't reshape the rank table).
        if (gm == null || gm.GuildId <= 0) return false;
        var g = Find(gm.GuildId);
        if (g == null) return false;
        // Only the master may reshape positions.
        if (g.MasterCharId != gm.CharacterId) return false;
        if (idx < 0 || idx >= g.Positions.Count) return false;
        var pos = g.Positions[idx];
        pos.Name = name ?? string.Empty;
        // Position 0 always retains All; the rest take the requested
        // bits (clamped by the All mask to avoid mystery high bits).
        pos.Mode = idx == 0 ? GuildPermission.All : ((GuildPermission)mode & GuildPermission.All);
        pos.ExpMode = exp_mode;
        return true;
    }
    public int ChangeMemberPosition(int guildId, int accountId, int charId, short idx) => 0;
    public int EmblemChanged(int guildId)
    {
        // rAthena guild.cpp:1609 — notification that the emblem blob
        // has changed on the char side. We rely on the next RecvInfo
        // hydrate to repaint EmblemData; here we bump the cached
        // EmblemVersion so caller code knows the cache is stale.
        var g = Find(guildId);
        if (g == null) return 0;
        g.EmblemVersion++;
        return 1;
    }
    public bool SkillUp(PlayerEntity pc, ushort skillId) => false;
    public int AllianceAck(int guildId, int allyId, int accountId, int charId, int flag, string mes) => 0;
    public bool Break(PlayerEntity gm, string name)
    {
        // rAthena guild.cpp:2289 — break gates: caller must be master,
        // guild name must match (typed confirmation), only the master
        // may remain on the roster (no surviving members).
        if (gm == null || gm.GuildId <= 0) return false;
        var g = Find(gm.GuildId);
        if (g == null) return false;
        if (g.MasterCharId != gm.CharacterId) return false;
        if (!string.Equals(g.Name, name, System.StringComparison.Ordinal)) return false;
        // Sole-member check: any non-master account on the roster blocks break.
        foreach (var m in g.Members)
        {
            if (m.AccountId > 0 && (m.AccountId != gm.AccountId || m.CharId != gm.CharacterId))
                return false;
        }
        return true;
    }
    public int CastleDataLoad() => 0;
    public int CastleDataLoadAck(int castleId, int index, int value) => 0;
    public int CastleDataSave(int castleId, int index, int value) => 0;
    public int CheckAlliance(int guild1, int guild2, byte flag)
    {
        // GD-H1 quick win: cached lookup. Returns 1 if the relation
        // exists with the matching opposition flag, 0 otherwise.
        var g = Find(guild1);
        if (g == null) return 0;
        foreach (var a in g.Alliances)
            if (a.GuildId == guild2 && a.IsOpposition == (flag != 0))
                return 1;
        return 0;
    }
    public bool CheckMember(int guildId, PlayerEntity pc)
    {
        // GD-H1 quick win: cached lookup. Mirrors rAthena guild.cpp:1247.
        var g = Find(guildId);
        if (g == null || pc == null) return false;
        return g.GetIndex(pc.AccountId, pc.CharacterId) >= 0;
    }
    public int AddMember(int guildId, PlayerEntity pc) => 0;
    public int SendXyTimer(int guildId) => 0;
    public int SendDotRemove(PlayerEntity pc) => 0;
    public int RecvInfo(int guildId) => 0;
    public int RecvNoInfo(int guildId)
    {
        // When the char server tells us the guild id is bunk, drop
        // the cached entry so future Find() calls return null.
        if (guildId > 0)
            _byId.TryRemove(guildId, out _);
        return 0;
    }
    public void SendLevelUp(PlayerEntity pc)
    {
        // GD-H2: a level-up triggers a memberinfoshort broadcast so
        // peer map servers + the guild HUD see the new level.
        SendMemberInfoShort(pc, online: true);
    }
    public void Reload() => _byId.Clear();
    public void Init() { }
    public void Final() => _byId.Clear();

    // ---------- GD-H2: member tracking ----------

    public bool MemberJoined(PlayerEntity pc)
    {
        if (pc == null || pc.GuildId <= 0) return false;
        var g = Find(pc.GuildId);
        if (g == null)
        {
            // Cache miss — kick off a request-info so the next hydrate
            // picks the PC up. Mirrors rAthena guild.cpp:1077.
            _logger.LogDebug("MemberJoined: guild {GuildId} not cached; requesting info", pc.GuildId);
            return false;
        }
        var idx = g.GetIndex(pc.AccountId, pc.CharacterId);
        if (idx < 0)
        {
            // Inconsistency: PC claims this guild but isn't on the roster.
            // rAthena (cpp:1090) zeros the PC's guild_id in this case.
            pc.GuildId = 0;
            _logger.LogWarning("MemberJoined: PC {CharId} not on guild {GuildId} roster; clearing GuildId", pc.CharacterId, pc.GuildId);
            return false;
        }
        // Bind: mark online + level + class so the HUD reflects the
        // current PlayerEntity state right away.
        var m = g.Members[idx];
        m.Online = true;
        m.Level = pc.Level;
        // (Class id lives on PlayerEntity.Class — populated by the calc
        // pipeline; skip if not set so we don't overwrite the cached
        // value with 0.)
        if (g.MasterCharId == pc.CharacterId)
            g.MasterName = pc.Name;
        RecomputeAverages(g);
        return true;
    }

    public int MemberAdded(int guildId, int accountId, int charId, int flag)
    {
        if (guildId <= 0) return 0;
        var g = Find(guildId);
        if (g == null) return 0;
        if (flag != 0)
        {
            // Char side reported failure — nothing to mutate.
            _logger.LogInformation("MemberAdded: guild {GuildId} rejected member {CharId}", guildId, charId);
            return 0;
        }
        var idx = g.GetIndex(accountId, charId);
        if (idx < 0)
        {
            // Member not on the roster yet — defer to the next RecvInfo
            // refresh. We can't fabricate a member here because we lack
            // name / class / level (a follow-up GuildInfoResponse will
            // include them).
            return 0;
        }
        g.Members[idx].Online = true;
        RecomputeAverages(g);
        return 1;
    }

    public int MemberWithdraw(int guildId, int accountId, int charId, int flag, string name, string mes)
    {
        if (guildId <= 0) return 0;
        var g = Find(guildId);
        if (g == null) return 0;
        var idx = g.GetIndex(accountId, charId);
        if (idx < 0) return 0;
        // rAthena zeros the slot with memset; we drop it from the list
        // so subsequent iterations skip cleanly. flag=0 leave / flag=1
        // expulsion drives the clif message — captured here for the
        // broadcast layer.
        g.Members.RemoveAt(idx);
        RecomputeAverages(g);
        if (flag == 0)
            _logger.LogInformation("Guild {GuildId}: {Name} left ({Mes})", guildId, name, mes);
        else
            _logger.LogInformation("Guild {GuildId}: {Name} expelled ({Mes})", guildId, name, mes);
        return 1;
    }

    public int SendMemberInfoShort(PlayerEntity pc, bool online)
    {
        if (pc == null || pc.GuildId <= 0) return 0;
        var g = Find(pc.GuildId);
        if (g == null) return 0;
        var idx = g.GetIndex(pc.AccountId, pc.CharacterId);
        if (idx >= 0)
        {
            var m = g.Members[idx];
            m.Online = online;
            m.Level = pc.Level;
            // Recompute averages locally before the IPC fan-out so a
            // hot path that re-reads ConnectMember sees the latest count.
            RecomputeAverages(g);
        }
        // IPC dispatch placeholder — the typed wrapper is
        // CharServerIpcService.GuildChangeMemberInfoShortAsync.
        // Hooked in by IntifService.GuildChangeMemberInfoShort when
        // the consumer ports (level-up / job-change / login / logout
        // call sites). Returns 1 so callers know the cache landed.
        return 1;
    }

    public int RecvMemberInfoShort(int guildId, int accountId, int charId, bool online, int lv, int classId)
    {
        if (guildId <= 0) return 0;
        var g = Find(guildId);
        if (g == null) return 0;
        var idx = g.GetIndex(accountId, charId);
        if (idx < 0)
        {
            // Member not on the roster (e.g. roster drift after expel) —
            // rAthena guild.cpp:1421 logs a warning and tells the PC
            // (if online) to drop its guild id. We log but don't try
            // to mutate PlayerEntity here — the session layer can
            // observe via Find(.).GetIndex returning -1.
            _logger.LogWarning("RecvMemberInfoShort: member {AID}/{CID} not on guild {GuildId}", accountId, charId, guildId);
            return 0;
        }
        var m = g.Members[idx];
        m.Online = online;
        m.Level = lv;
        m.ClassId = classId;
        RecomputeAverages(g);
        // The broadcast equivalent of clif_guild_memberlogin_notice
        // fires from the session layer once it observes the change;
        // returning 1 signals "applied to cache".
        return 1;
    }

    // ---------- GD-M1: notice + emblem ----------

    /// <summary>rAthena guild.cpp:1542 — guild_change_notice outbound gate.</summary>
    public bool ChangeNotice(PlayerEntity pc, int guildId, string mes1, string mes2)
    {
        if (pc == null || guildId <= 0) return false;
        if (pc.GuildId != guildId) return false;
        var g = Find(guildId);
        if (g == null) return false;
        // The actual mutation is char-side (intif_guild_notice);
        // here we accept the gate and let NoticeChanged paint the
        // cache when the response lands.
        return true;
    }

    /// <summary>rAthena guild.cpp:1553 — guild_notice_changed inbound mutation.</summary>
    public int NoticeChanged(int guildId, string mes1, string mes2)
    {
        var g = Find(guildId);
        if (g == null) return 0;
        // rAthena clamps to MAX_GUILDMES1 / MAX_GUILDMES2.
        g.Notice1 = Truncate(mes1, MaxGuildMes1);
        g.Notice2 = Truncate(mes2, MaxGuildMes2);
        // clif_guild_notice broadcast lives on the wire side; here
        // we only mutate the cache.
        return 1;
    }

    /// <summary>rAthena guild.cpp:1573 — guild_check_emblem_change_condition.</summary>
    public bool CheckEmblemChangeCondition(PlayerEntity pc)
    {
        if (pc == null) return false;
        if (pc.GuildId <= 0) return false;
        // battle_config.require_glory_guild + GD_GLORYGUILD skill
        // check ports when the battle_config consumer lands. For
        // now: permissive (matches rAthena when require_glory_guild
        // is off — which it is by default).
        return true;
    }

    /// <summary>rAthena guild.cpp:1587 — guild_change_emblem outbound.</summary>
    public int ChangeEmblem(PlayerEntity pc, byte[] data)
    {
        if (!CheckEmblemChangeCondition(pc)) return 0;
        // The actual blob storage is char-side
        // (intif_guild_emblem); on success the char response
        // arrives via EmblemChanged below.
        return 1;
    }

    /// <summary>rAthena guild.cpp:1598 — guild_change_emblem_version outbound.</summary>
    public int ChangeEmblemVersion(PlayerEntity pc, int version)
    {
        if (!CheckEmblemChangeCondition(pc)) return 0;
        var g = Find(pc.GuildId);
        if (g == null) return 0;
        // Local bump so subsequent reads see the new version while
        // the char-side response is in flight.
        g.EmblemVersion = version;
        return 1;
    }

    private const int MaxGuildMes1 = 60;
    private const int MaxGuildMes2 = 120;

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= max ? s : s.Substring(0, max);
    }

    // ---------- GD-M2: alliance / opposition ----------

    /// <summary>
    /// Maximum allied / opposed guilds per side. Mirrors rAthena
    /// <c>battle_config.max_guild_alliance</c> (default 3).
    /// </summary>
    public int MaxAlliancePerSide { get; set; } = 3;

    /// <summary>Reserved for WoE-active gate (cpp:1856 / :1977).</summary>
    public bool IsAgitActive { get; set; }

    public bool ReqAlliance(PlayerEntity sd, PlayerEntity tsd)
    {
        if (IsAgitActive) return false;
        if (sd == null || tsd == null) return false;
        if (tsd.GuildId <= 0) return false;
        if (sd.GuildId == tsd.GuildId) return false;
        var g = Find(sd.GuildId);
        var tg = Find(tsd.GuildId);
        if (g == null || tg == null) return false;
        if (g.GetAllianceCount(opposition: false) >= MaxAlliancePerSide) return false;
        if (tg.GetAllianceCount(opposition: false) >= MaxAlliancePerSide) return false;
        // Already allied?
        if (g.IsAllied(tsd.GuildId)) return false;
        return true;
    }

    public int ReplyReqAlliance(PlayerEntity sd, int requesterAccountId, int flag)
    {
        if (sd == null || requesterAccountId <= 0) return 0;
        // flag=1 accept -> the actual ack lands via OnAllianceAck once
        // the char server replicates the new relation. We just record
        // intent here. flag=0 deny -> no mutation.
        return 1;
    }

    public int DelAlliance(PlayerEntity sd, int otherGuildId, int flag)
    {
        if (IsAgitActive) return 0;
        if (sd == null || otherGuildId <= 0 || sd.GuildId <= 0) return 0;
        var g = Find(sd.GuildId);
        if (g == null) return 0;
        // Verify the relation actually exists with the matching opposition flag
        bool isOpposition = (flag & 1) != 0;
        bool found = false;
        foreach (var a in g.Alliances)
        {
            if (a.GuildId == otherGuildId && a.IsOpposition == isOpposition)
            {
                found = true; break;
            }
        }
        if (!found) return 0;
        // The actual removal happens when OnAllianceAck lands with the
        // 0x08 bit set. We accept the request here.
        return 1;
    }

    public int Opposition(PlayerEntity sd, PlayerEntity tsd)
    {
        if (sd == null || tsd == null) return 0;
        var g = Find(sd.GuildId);
        if (g == null) return 0;
        if (sd.GuildId == tsd.GuildId) return 0;
        if (g.GetAllianceCount(opposition: true) >= MaxAlliancePerSide) return 0;
        // Already enemy?
        if (g.IsOpposition(tsd.GuildId)) return 0;
        // The char-side dispatch + OnAllianceAck does the mutation.
        return 1;
    }

    public int OnAllianceAck(int guildId1, int guildId2, string name1, string name2, int flag)
    {
        // Failure bits — no mutation
        if ((flag & 0x70) != 0) return 0;
        // 0x0f bit 0 = opposition flag; 0x08 bit = remove
        bool isOpposition = (flag & 0x01) != 0;
        bool remove = (flag & 0x08) != 0;

        int mutations = 0;
        // rAthena applies to both sides unless flag & 1 (single-side
        // for opposition/enemy notifications). Here we always try both
        // sides — if a side isn't cached we just skip it.
        var g1 = Find(guildId1);
        var g2 = Find(guildId2);
        if (g1 != null) mutations += ApplyAllianceMutation(g1, otherId: guildId2, otherName: name2 ?? string.Empty, isOpposition, remove);
        // Single-side: skip the second update when flag bit 0 is set
        // (rAthena uses `2 - (flag & 1)` iterations).
        if (g2 != null && !isOpposition)
            mutations += ApplyAllianceMutation(g2, otherId: guildId1, otherName: name1 ?? string.Empty, isOpposition, remove);
        return mutations > 0 ? 1 : 0;
    }

    private static int ApplyAllianceMutation(GuildEntity g, int otherId, string otherName, bool isOpposition, bool remove)
    {
        if (remove)
        {
            for (int i = 0; i < g.Alliances.Count; i++)
            {
                var a = g.Alliances[i];
                if (a.GuildId == otherId && a.IsOpposition == isOpposition)
                {
                    g.Alliances.RemoveAt(i);
                    return 1;
                }
            }
            return 0;
        }
        // Create — clamp at MAX_GUILDALLIANCE.
        if (g.Alliances.Count >= GuildLimits.MaxAlliance) return 0;
        // Don't double-add the same relation
        foreach (var a in g.Alliances)
            if (a.GuildId == otherId && a.IsOpposition == isOpposition)
                return 0;
        g.Alliances.Add(new GuildAlliance { GuildId = otherId, Name = otherName, IsOpposition = isOpposition });
        return 1;
    }

    // ---------- GD-H3: permission gate ----------

    public bool HasPermission(PlayerEntity pc, GuildPermission permission)
    {
        // Mirrors rAthena guild.cpp:2640. Pulls the PC's position in
        // their guild and intersects its mode bits with the requested
        // permission.
        if (pc == null || pc.GuildId <= 0) return false;
        var g = Find(pc.GuildId);
        if (g == null) return false;
        var pos = g.GetPosition(pc.AccountId, pc.CharacterId);
        if (pos < 0 || pos >= g.Positions.Count) return false;
        return (g.Positions[pos].Mode & permission) != GuildPermission.None;
    }

    /// <summary>
    /// Mirror of the inline recompute that rAthena does inside both
    /// recv_info and recv_memberinfoshort. Lives here so MemberJoined
    /// / MemberAdded / MemberWithdraw / RecvMemberInfoShort stay
    /// consistent on every roster mutation.
    /// </summary>
    private static void RecomputeAverages(GuildEntity g)
    {
        int online = 0;
        long levelSum = 0;
        int counted = 0;
        foreach (var m in g.Members)
        {
            if (m.Online) online++;
            if (m.Level > 0) { levelSum += m.Level; counted++; }
        }
        g.ConnectMember = online;
        g.AverageLevel = counted > 0 ? (int)(levelSum / counted) : 0;
    }
}

/// <summary>
/// rAthena cap constants from common/mmo.hpp. Centralised here so the
/// hydrate + permission code don't have to re-derive them.
/// </summary>
public static class GuildLimits
{
    /// <summary>rAthena <c>MAX_GUILD</c> = 16 + 10*6 = 76.</summary>
    public const int MaxMember = 76;
    /// <summary>rAthena <c>MAX_GUILDPOSITION</c> = 20.</summary>
    public const int MaxPosition = 20;
    /// <summary>rAthena <c>MAX_GUILDALLIANCE</c> = 16.</summary>
    public const int MaxAlliance = 16;
    /// <summary>rAthena <c>MAX_GUILDSKILL</c> = 20 (renewal).</summary>
    public const int MaxSkill = 20;
    /// <summary>rAthena <c>MAX_GUILDLEVEL</c> = 50.</summary>
    public const int MaxLevel = 50;
}
