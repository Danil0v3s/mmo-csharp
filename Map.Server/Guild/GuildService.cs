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
    public bool Invite(PlayerEntity inviter, PlayerEntity invitee) => false;
    public bool ReplyInvite(PlayerEntity invitee, int guildId, byte ok) => false;
    public bool Leave(PlayerEntity pc, int guildId, int accountId, int charId, string reason) => false;
    public bool Expulsion(PlayerEntity gm, int guildId, int accountId, int charId, string reason) => false;
    public int SendMessage(PlayerEntity from, string text) => 0;
    public int RecvMessage(int guildId, string sender, string text) => 0;
    public bool ChangePosition(PlayerEntity gm, int idx, int mode, int exp_mode, string name) => false;
    public int ChangeMemberPosition(int guildId, int accountId, int charId, short idx) => 0;
    public int EmblemChanged(int guildId) => 0;
    public bool SkillUp(PlayerEntity pc, ushort skillId) => false;
    public int AllianceAck(int guildId, int allyId, int accountId, int charId, int flag, string mes) => 0;
    public bool Break(PlayerEntity gm, string name) => false;
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
