using Map.Server.Entities;
using Map.Server.Guild;
using Map.Server.Services;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@breakguild &lt;guild name&gt;</c> — disband your guild (master only).
/// rAthena <c>atcommand_breakguild</c> (atcommand.cpp). Forwards to
/// <see cref="IGuildService.Break"/> which enforces master + name-match +
/// sole-member gates.
/// </summary>
public sealed class BreakGuildCommand(
    IGuildService guilds,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "breakguild";
    public string Description => "@breakguild <guild name> — disband your guild (master only).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@breakguild: usage — @breakguild <guild name>");
            return Task.CompletedTask;
        }
        var name = string.Join(' ', args);
        if (guilds.Break(caller, name))
            GmCommandReply.Send(visibility, caller, $"@breakguild: '{name}' disbanded.");
        else
            GmCommandReply.Send(visibility, caller, $"@breakguild: refused — not master, wrong name, or members remain.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@guildstorage</c> — open the guild storage window. rAthena
/// <c>atcommand_guildstorage</c>. Forwards to
/// <see cref="IGuildStorageService.Open"/>; the storage flow already
/// handles the GUILD_PERM_STORAGE check.
/// </summary>
public sealed class GuildStorageCommand(
    IGuildService guilds,
    Map.Server.Storage.Guild.IGuildStorageService storage,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "guildstorage";
    public string Description => "@guildstorage — open your guild storage.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (caller.GuildId <= 0)
        {
            GmCommandReply.Send(visibility, caller, "@guildstorage: you are not in a guild.");
            return Task.CompletedTask;
        }
        if (!guilds.HasPermission(caller, GuildPermission.Storage))
        {
            GmCommandReply.Send(visibility, caller, "@guildstorage: insufficient permission.");
            return Task.CompletedTask;
        }
        storage.Open(caller);
        GmCommandReply.Send(visibility, caller, "@guildstorage: opened.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@cleargstorage</c> — empty the guild storage. Master-only.
/// rAthena <c>atcommand_cleargstorage</c>.
/// </summary>
public sealed class ClearGuildStorageCommand(
    IGuildService guilds,
    Map.Server.Storage.Guild.IGuildStorageService storage,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "cleargstorage";
    public string Description => "@cleargstorage — empty the guild storage (master only).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (caller.GuildId <= 0)
        {
            GmCommandReply.Send(visibility, caller, "@cleargstorage: you are not in a guild.");
            return Task.CompletedTask;
        }
        var g = guilds.Find(caller.GuildId);
        if (g == null || g.MasterCharId != caller.CharacterId)
        {
            GmCommandReply.Send(visibility, caller, "@cleargstorage: master only.");
            return Task.CompletedTask;
        }
        // Clear-storage hands off to the typed wrapper when a delete
        // path lands; the IGuildStorageService surface exposes
        // Delete(guildId) for full removal (used on guild break).
        storage.Delete(caller.GuildId);
        GmCommandReply.Send(visibility, caller, "@cleargstorage: guild storage cleared.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@changegm &lt;char name&gt;</c> — transfer guild leadership to a
/// guild member. rAthena <c>atcommand_changegm</c>. Forwards to
/// <see cref="IGuildService.GmChange"/>.
/// </summary>
public sealed class ChangeGmCommand(
    IGuildService guilds,
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "changegm";
    public string Description => "@changegm <char name> — transfer guild leadership.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@changegm: usage — @changegm <char name>");
            return Task.CompletedTask;
        }
        if (caller.GuildId <= 0)
        {
            GmCommandReply.Send(visibility, caller, "@changegm: you are not in a guild.");
            return Task.CompletedTask;
        }
        var g = guilds.Find(caller.GuildId);
        if (g == null || g.MasterCharId != caller.CharacterId)
        {
            GmCommandReply.Send(visibility, caller, "@changegm: master only.");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@changegm: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        if (target.GuildId != caller.GuildId)
        {
            GmCommandReply.Send(visibility, caller, $"@changegm: '{target.Name}' is not in your guild.");
            return Task.CompletedTask;
        }
        if (guilds.GmChange(caller.GuildId, target.CharacterId))
            GmCommandReply.Send(visibility, caller, $"@changegm: leadership transfer to {target.Name} dispatched.");
        else
            GmCommandReply.Send(visibility, caller, "@changegm: transfer refused (already master or not a member).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@guildlevelup [n]</c> — grant your guild N skill points (defaults
/// to 1). rAthena <c>atcommand_guildlevelup</c>. Direct mutation on
/// the cached <see cref="GuildEntity.SkillPoints"/> — `@skillpoint` style.
/// </summary>
public sealed class GuildLevelUpCommand(
    IGuildService guilds,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "guildlevelup";
    public string Description => "@guildlevelup [n] — grant your guild N skill points (default 1).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (caller.GuildId <= 0)
        {
            GmCommandReply.Send(visibility, caller, "@guildlevelup: you are not in a guild.");
            return Task.CompletedTask;
        }
        var g = guilds.Find(caller.GuildId);
        if (g == null)
        {
            GmCommandReply.Send(visibility, caller, "@guildlevelup: guild not cached.");
            return Task.CompletedTask;
        }
        int n = 1;
        if (args.Count > 0 && int.TryParse(args[0], out var parsed)) n = Math.Clamp(parsed, 1, 50);
        g.SkillPoints += n;
        if (g.GuildLv < GuildLimits.MaxLevel) g.GuildLv = Math.Min(g.GuildLv + 1, GuildLimits.MaxLevel);
        GmCommandReply.Send(visibility, caller, $"@guildlevelup: guild now lv {g.GuildLv}, +{n} skill points (total {g.SkillPoints}).");
        return Task.CompletedTask;
    }
}
