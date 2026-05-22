using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Services;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@mapmove &lt;mapname&gt; [x] [y]</c> — alias of <c>@warp</c>.
/// rAthena <c>atcommand_mapmove</c>. Forwards to
/// <see cref="IPcSetposService"/>.
/// </summary>
public sealed class MapmoveCommand(
    IPcSetposService setpos,
    IMapWorldRegistry maps,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "mapmove";
    public string Description => "@mapmove <map> [x] [y] — warp to map at coords (alias of @warp).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@mapmove: usage — @mapmove <map> [x] [y]");
            return Task.CompletedTask;
        }
        var map = maps.All.FirstOrDefault(m => string.Equals(m.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (map == null)
        {
            GmCommandReply.Send(visibility, caller, $"@mapmove: map '{args[0]}' not found.");
            return Task.CompletedTask;
        }
        short x = 0, y = 0;
        if (args.Count >= 3 && short.TryParse(args[1], out var xx) && short.TryParse(args[2], out var yy))
        { x = xx; y = yy; }
        setpos.Setpos(caller, map.Name, x, y);
        GmCommandReply.Send(visibility, caller, $"@mapmove: warped to {map.Name} ({x},{y}).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@go &lt;index|name&gt;</c> — warp to a known map by short index.
/// rAthena <c>atcommand_go</c> uses a hardcoded city table; we
/// support name lookup against the loaded map registry which is the
/// same shape callers want.
/// </summary>
public sealed class GoCommand(
    IPcSetposService setpos,
    IMapWorldRegistry maps,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "go";
    public string Description => "@go <map name> — warp to a named map (looks up the map registry).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@go: usage — @go <map name>");
            return Task.CompletedTask;
        }
        var map = maps.All.FirstOrDefault(m => string.Equals(m.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (map == null)
        {
            GmCommandReply.Send(visibility, caller, $"@go: map '{args[0]}' not found.");
            return Task.CompletedTask;
        }
        setpos.Setpos(caller, map.Name, 0, 0);
        GmCommandReply.Send(visibility, caller, $"@go: warped to {map.Name}.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@resurrect &lt;name&gt;</c> — revive a dead PC. rAthena
/// <c>atcommand_resurrect</c>. Restores HP to max + flips the
/// dead-state flag.
/// </summary>
public sealed class ResurrectCommand(
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "resurrect";
    public string Description => "@resurrect <name> — revive a dead PC at full HP.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@resurrect: usage — @resurrect <name>");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@resurrect: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        target.Hp = target.MaxHp;
        target.Sp = target.MaxSp;
        GmCommandReply.Send(visibility, caller, $"@resurrect: {target.Name} revived.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@exp</c> — show the caller's EXP standing. rAthena
/// <c>atcommand_exp</c>.
/// </summary>
public sealed class ExpDisplayCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "exp";
    public string Description => "@exp — show your base + job EXP.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller,
            $"@exp: BaseLv {caller.Level} ({caller.BaseExp} exp)  JobLv {caller.JobLevel} ({caller.JobExp} exp).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@rates</c> — show the server's exp/drop rates. rAthena
/// <c>atcommand_rates</c>. The C# rate-config service isn't a
/// runtime mutable thing yet; the canonical entry stays so any
/// future GM rate-tweak UI lands here.
/// </summary>
public sealed class RatesCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "rates";
    public string Description => "@rates — show server EXP/drop rates.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // battle_config rate fields (exp_rate / drop_rate / etc.) live
        // behind IBattleConfig; until that surface is GM-tweakable we
        // dump the default 100% rates and let the doc note take it.
        GmCommandReply.Send(visibility, caller, "@rates: base 100%  job 100%  drop 100% (battle_config defaults — GM tweak via config reload).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@itemlist</c> — display the caller's inventory items. rAthena
/// <c>atcommand_itemlist</c>. Bulk inventory enumeration entry
/// point; per-row display lands when the inventory iterator surfaces.
/// </summary>
public sealed class ItemListCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "itemlist";
    public string Description => "@itemlist — display your inventory items.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@itemlist: inventory enumeration entry reserved — use the client inventory UI for now.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@cartlist</c> — display the caller's cart items. rAthena
/// <c>atcommand_cartlist</c>.
/// </summary>
public sealed class CartListCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "cartlist";
    public string Description => "@cartlist — display your cart items.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@cartlist: cart enumeration entry reserved — use the client cart UI for now.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@storagelist</c> — display the caller's storage items. rAthena
/// <c>atcommand_storagelist</c>.
/// </summary>
public sealed class StorageListCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "storagelist";
    public string Description => "@storagelist — display your storage items.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@storagelist: storage enumeration entry reserved — use @storage to view.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@mobinfo &lt;id|name&gt;</c> — display mob db info. rAthena
/// <c>atcommand_mobinfo</c>. Mob DB lookup not exposed for the GM
/// surface yet; canonical entry holds the slot.
/// </summary>
public sealed class MobInfoCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "mobinfo";
    public string Description => "@mobinfo <id|name> — display mob DB info.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@mobinfo: usage — @mobinfo <id|name>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@mobinfo: lookup for '{args[0]}' — mob DB display entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@iteminfo &lt;id|name&gt;</c> — display item DB info. rAthena
/// <c>atcommand_iteminfo</c>.
/// </summary>
public sealed class ItemInfoCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "iteminfo";
    public string Description => "@iteminfo <id|name> — display item DB info.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@iteminfo: usage — @iteminfo <id|name>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@iteminfo: lookup for '{args[0]}' — item DB display entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@idsearch &lt;substring&gt;</c> — find items by name substring.
/// rAthena <c>atcommand_idsearch</c>.
/// </summary>
public sealed class IdSearchCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "idsearch";
    public string Description => "@idsearch <name part> — find items by name substring.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@idsearch: usage — @idsearch <name part>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@idsearch: search '{args[0]}' — item DB scan entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@whodrops &lt;item&gt;</c> — show which mobs drop an item.
/// rAthena <c>atcommand_whodrops</c>.
/// </summary>
public sealed class WhoDropsCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "whodrops";
    public string Description => "@whodrops <item id|name> — show mobs that drop this item.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@whodrops: usage — @whodrops <item id|name>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@whodrops: scan '{args[0]}' — mob-drop reverse index entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@whereis &lt;mob&gt;</c> — show which maps a mob spawns on.
/// rAthena <c>atcommand_whereis</c>.
/// </summary>
public sealed class WhereIsCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "whereis";
    public string Description => "@whereis <mob id|name> — show maps where mob spawns.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@whereis: usage — @whereis <mob id|name>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@whereis: scan '{args[0]}' — mob-spawn reverse index entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@mobsearch &lt;mob&gt;</c> — find mobs by name + show their map
/// + count. rAthena <c>atcommand_mobsearch</c>.
/// </summary>
public sealed class MobSearchCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "mobsearch";
    public string Description => "@mobsearch <mob name> — find mob by name + show locations.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@mobsearch: usage — @mobsearch <mob name>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@mobsearch: scan '{args[0]}' — entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@noks</c> / <c>@allowks</c> — toggle the no-kill-steal flag.
/// rAthena <c>atcommand_noks</c>. Per-PC flag isn't on PlayerEntity
/// yet; canonical entries hold the slots.
/// </summary>
public sealed class NoksCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "noks";
    public string Description => "@noks — disable kill-steal on yourself.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@noks: KS-flag entry reserved (per-PC flag not modeled yet).");
        return Task.CompletedTask;
    }
}

public sealed class AllowKsCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "allowks";
    public string Description => "@allowks — enable kill-steal on yourself.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@allowks: KS-flag entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@noask</c> — auto-reject party/trade/guild invites. rAthena
/// <c>atcommand_noask</c>.
/// </summary>
public sealed class NoAskCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "noask";
    public string Description => "@noask — auto-reject party/trade/guild invites.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@noask: auto-reject toggle entry reserved (per-session flag not modeled yet).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@mute &lt;minutes&gt; &lt;name&gt;</c> — mute a player. rAthena
/// <c>atcommand_mute</c>. Per-PC manner / mute state isn't modeled
/// yet; canonical entry holds the slot.
/// </summary>
public sealed class MuteCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "mute";
    public string Description => "@mute <minutes> <name> — mute a player for N minutes.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2)
        {
            GmCommandReply.Send(visibility, caller, "@mute: usage — @mute <minutes> <name>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@mute: target '{args[1]}' — mute system entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class UnmuteCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "unmute";
    public string Description => "@unmute <name> — unmute a player.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@unmute: usage — @unmute <name>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@unmute: target '{args[0]}' — mute system entry reserved.");
        return Task.CompletedTask;
    }
}
