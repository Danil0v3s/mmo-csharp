using System.Collections.Generic;

namespace Map.Server.Guild;

/// <summary>
/// Map-server in-memory replica of a guild. Mirrors rAthena's
/// <c>MapGuild</c> wrapper around <c>struct mmo_guild</c>
/// (common/mmo.hpp:855). The authoritative copy lives on char-server
/// (<c>guild</c> + <c>guild_member</c> + <c>guild_position</c> +
/// <c>guild_alliance</c> tables); we mirror it here for hot-path
/// reads (member lookup, alliance check, has-permission) without a
/// blocking IPC round-trip per call.
///
/// Hydrate via <c>IGuildService.OnRecvInfo(GuildInfoData)</c>; mutate
/// via the explicit incremental ops (member info short / position
/// change / alliance ack / notice change / emblem change). Reload at
/// any time by re-issuing <c>GuildRequestInfo</c>.
/// </summary>
public sealed class GuildEntity
{
    /// <summary>rAthena <c>g.guild_id</c>.</summary>
    public int GuildId { get; set; }
    /// <summary>rAthena <c>g.name</c>.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>rAthena <c>g.guild_lv</c>.</summary>
    public int GuildLv { get; set; }
    /// <summary>rAthena <c>g.max_member</c> (MAX_GUILD = 76).</summary>
    public int MaxMember { get; set; }
    /// <summary>Char-id of the guild master (matches member at <see cref="Members"/>[0] by convention).</summary>
    public int MasterCharId { get; set; }
    /// <summary>Master's display name (mirrors <c>g.master</c>).</summary>
    public string MasterName { get; set; } = string.Empty;
    /// <summary>rAthena <c>g.emblem_id</c> / <c>g.emblem_len</c>; we use Version as the id and Data as the blob.</summary>
    public int EmblemVersion { get; set; }
    /// <summary>Emblem image bytes (BMP). Empty when no emblem set.</summary>
    public byte[] EmblemData { get; set; } = System.Array.Empty<byte>();
    /// <summary>rAthena <c>g.mes1</c> — first line of the guild notice (60 chars).</summary>
    public string Notice1 { get; set; } = string.Empty;
    /// <summary>rAthena <c>g.mes2</c> — second line of the guild notice (120 chars).</summary>
    public string Notice2 { get; set; } = string.Empty;
    /// <summary>rAthena <c>g.average_lv</c>, recomputed from <see cref="Members"/> on every memberinfoshort update.</summary>
    public int AverageLevel { get; set; }
    /// <summary>rAthena <c>g.connect_member</c> — count of online members.</summary>
    public int ConnectMember { get; set; }
    /// <summary>rAthena <c>g.skill_point</c> — unspent guild skill points.</summary>
    public int SkillPoints { get; set; }

    /// <summary>
    /// Member list, ordered by index. Index 0 is the guild master.
    /// Mirrors <c>g.member[MAX_GUILD]</c>; we use a List for
    /// resize-on-grow but cap at <see cref="MaxMember"/> on insert.
    /// </summary>
    public List<GuildMember> Members { get; } = new();

    /// <summary>
    /// Position table, indexed 0..MAX_GUILDPOSITION-1. Index 0 is the
    /// master position with implicit <see cref="GuildPermission.All"/>.
    /// </summary>
    public List<GuildPosition> Positions { get; } = new();

    /// <summary>
    /// Alliance + opposition table. Mirrors <c>g.alliance[MAX_GUILDALLIANCE]</c>.
    /// </summary>
    public List<GuildAlliance> Alliances { get; } = new();

    /// <summary>Find a member by AID + CID. Returns -1 if not found. Mirrors rAthena <c>guild_getindex</c> (guild.cpp:584).</summary>
    public int GetIndex(int accountId, int charId)
    {
        for (int i = 0; i < Members.Count; i++)
        {
            var m = Members[i];
            if (m.AccountId == accountId && m.CharId == charId)
                return i;
        }
        return -1;
    }

    /// <summary>Helper: return the member's position index, or -1.</summary>
    public int GetPosition(int accountId, int charId)
    {
        var idx = GetIndex(accountId, charId);
        return idx < 0 ? -1 : Members[idx].Position;
    }

    /// <summary>Returns true if this guild lists the given guild id as an ally (opposition=false). Mirrors <c>guild_isallied</c> (guild.cpp:2630).</summary>
    public bool IsAllied(int otherGuildId)
    {
        foreach (var a in Alliances)
            if (a.GuildId == otherGuildId && !a.IsOpposition)
                return true;
        return false;
    }

    /// <summary>Returns true if this guild lists the given guild id as an enemy (opposition=true).</summary>
    public bool IsOpposition(int otherGuildId)
    {
        foreach (var a in Alliances)
            if (a.GuildId == otherGuildId && a.IsOpposition)
                return true;
        return false;
    }

    /// <summary>Count allies (flag=0) or enemies (flag=1). Mirrors <c>guild_get_alliance_count</c> (guild.cpp:1813).</summary>
    public int GetAllianceCount(bool opposition)
    {
        int c = 0;
        foreach (var a in Alliances)
            if (a.GuildId > 0 && a.IsOpposition == opposition)
                c++;
        return c;
    }

    /// <summary>Look up a guild skill's learned level. 0 if not learned.</summary>
    public int GetSkillLevel(ushort skillId)
        => Skills.TryGetValue(skillId, out var lv) ? lv : 0;

    /// <summary>
    /// Per-guild skill level table. Mirrors <c>g.skill[MAX_GUILDSKILL]</c>
    /// but indexed by absolute skill id (GD_*) for direct lookup.
    /// </summary>
    public Dictionary<ushort, int> Skills { get; } = new();
}

/// <summary>A single guild member slot. Mirrors rAthena <c>struct guild_member</c>.</summary>
public sealed class GuildMember
{
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public int Level { get; set; }
    /// <summary>Position index (0..MAX_GUILDPOSITION-1).</summary>
    public int Position { get; set; }
    /// <summary>Online flag from char-server. False when the member is logged out.</summary>
    public bool Online { get; set; }
    /// <summary>Accumulated unflushed guild EXP (rAthena <c>m.exp</c>); flushed periodically by the payexp timer.</summary>
    public long Exp { get; set; }
}

/// <summary>A guild rank/position. Mirrors rAthena <c>struct guild_position</c>.</summary>
public sealed class GuildPosition
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Permission bitmask — see <see cref="GuildPermission"/>.</summary>
    public GuildPermission Mode { get; set; }
    /// <summary>EXP-tax bitmask; non-zero means members at this position pay guild EXP tax.</summary>
    public int ExpMode { get; set; }
}

/// <summary>A guild relationship entry. Mirrors rAthena <c>struct guild_alliance</c>.</summary>
public sealed class GuildAlliance
{
    public int GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>False = allied (opposition flag 0); true = enemy (flag 1).</summary>
    public bool IsOpposition { get; set; }
}
