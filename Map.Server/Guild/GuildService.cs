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
    public void SendLevelUp(PlayerEntity pc) { }
    public void Reload() => _byId.Clear();
    public void Init() { }
    public void Final() => _byId.Clear();
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
