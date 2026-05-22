using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Services;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@identifyall</c> — flip every inventory row to identified.
/// rAthena <c>atcommand_identifyall</c>. Forwards to
/// <see cref="IPlayerInventoryHelpers.IdentifyAll"/>.
/// </summary>
public sealed class IdentifyAllCommand(
    IPlayerInventoryHelpers inv,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "identifyall";
    public string Description => "@identifyall — identify every inventory row.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var n = inv.IdentifyAll(caller);
        GmCommandReply.Send(visibility, caller, $"@identifyall: {n} items identified.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@itemreset</c> — clear non-equipped inventory. rAthena
/// <c>atcommand_itemreset</c>. The bulk-delete entry point lives on
/// the inventory persistence layer; this canonical command stays so
/// the GM/script path resolves when the helper ships.
/// </summary>
public sealed class ItemResetCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "itemreset";
    public string Description => "@itemreset — drop every non-equipped inventory row.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@itemreset: bulk inventory delete is pending — entry point reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@dropall [type]</c> — drop every inventory item on the ground.
/// rAthena <c>atcommand_dropall</c>.
/// </summary>
public sealed class DropAllCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "dropall";
    public string Description => "@dropall — drop every non-equipped item on the ground.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@dropall: bulk inventory drop is pending — entry point reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@storeall</c> — move every non-equipped inventory item to
/// account storage. rAthena <c>atcommand_storeall</c>.
/// </summary>
public sealed class StoreAllCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "storeall";
    public string Description => "@storeall — move every non-equipped item to account storage.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@storeall: bulk move-to-storage is pending — entry point reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@clearcart</c> — empty the caller's cart. rAthena
/// <c>atcommand_clearcart</c>. Best-effort per-slot delete via
/// <see cref="IPlayerInventoryHelpers.CartDelItem"/> across the
/// rAthena MAX_CART (100) range.
/// </summary>
public sealed class ClearCartCommand(
    IPlayerInventoryHelpers inv,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "clearcart";
    public string Description => "@clearcart — empty your cart.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int removed = 0;
        for (int i = 0; i < 100; i++)
            if (inv.CartDelItem(caller, i, int.MaxValue)) removed++;
        GmCommandReply.Send(visibility, caller, $"@clearcart: {removed} cart rows cleared.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@clearstorage</c> — empty the caller's account storage. rAthena
/// <c>atcommand_clearstorage</c>.
/// </summary>
public sealed class ClearStorageCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "clearstorage";
    public string Description => "@clearstorage — empty your account storage.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@clearstorage: bulk-clear endpoint is pending — entry point reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@repair &lt;index&gt;</c> — repair a broken inventory item.
/// rAthena <c>atcommand_repair</c>. Per-item durability flag isn't
/// modeled yet; canonical entry stays so the GM path is consistent.
/// </summary>
public sealed class RepairCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "repair";
    public string Description => "@repair <inventory slot> — repair a broken item.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var idx))
        {
            GmCommandReply.Send(visibility, caller, "@repair: usage — @repair <inventory slot>");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, $"@repair: slot {idx} processed (per-item Broken flag pending).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@repairall</c> — repair every broken item. rAthena
/// <c>atcommand_repairall</c>.
/// </summary>
public sealed class RepairAllCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "repairall";
    public string Description => "@repairall — repair every broken item.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@repairall: processed (per-item Broken flag pending).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@jail &lt;name&gt;</c> — send a player to jail. rAthena
/// <c>atcommand_jail</c>. Forwards to <see cref="IPlayerJailService.Jail"/>.
/// </summary>
public sealed class JailCommand(
    IPlayerJailService jail,
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "jail";
    public string Description => "@jail <name> — send a player to jail (indefinite).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@jail: usage — @jail <name>");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@jail: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        if (jail.Jail(target, minutes: 0))
            GmCommandReply.Send(visibility, caller, $"@jail: {target.Name} jailed (indefinite).");
        else
            GmCommandReply.Send(visibility, caller, "@jail: refused.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@unjail &lt;name&gt;</c> — release a jailed player. rAthena
/// <c>atcommand_unjail</c>.
/// </summary>
public sealed class UnjailCommand(
    IPlayerJailService jail,
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "unjail";
    public string Description => "@unjail <name> — release a jailed player.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@unjail: usage — @unjail <name>");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@unjail: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        if (jail.Unjail(target))
            GmCommandReply.Send(visibility, caller, $"@unjail: {target.Name} released.");
        else
            GmCommandReply.Send(visibility, caller, "@unjail: refused (not jailed?).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@jailfor &lt;minutes&gt; &lt;name&gt;</c> — timed jail. rAthena
/// <c>atcommand_jailfor</c>.
/// </summary>
public sealed class JailForCommand(
    IPlayerJailService jail,
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "jailfor";
    public string Description => "@jailfor <minutes> <name> — jail for N minutes.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2 || !int.TryParse(args[0], out var mins) || mins < 0)
        {
            GmCommandReply.Send(visibility, caller, "@jailfor: usage — @jailfor <minutes> <name>");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[1], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@jailfor: '{args[1]}' not online.");
            return Task.CompletedTask;
        }
        if (jail.Jail(target, mins))
            GmCommandReply.Send(visibility, caller, $"@jailfor: {target.Name} jailed for {mins} min.");
        else
            GmCommandReply.Send(visibility, caller, "@jailfor: refused.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@jailtime</c> — display the caller's remaining jail time.
/// rAthena <c>atcommand_jailtime</c>.
/// </summary>
public sealed class JailTimeCommand(
    IPlayerJailService jail,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "jailtime";
    public string Description => "@jailtime — display your jail time.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (!jail.IsJailed(caller))
        {
            GmCommandReply.Send(visibility, caller, "@jailtime: you are not jailed.");
            return Task.CompletedTask;
        }
        GmCommandReply.Send(visibility, caller, "@jailtime: jailed (remaining time tracked server-side).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@jobchange &lt;classId&gt;</c> — change the caller's class.
/// rAthena <c>atcommand_jobchange</c>. Forwards to
/// <see cref="IJobChangeService.Change"/>.
/// </summary>
public sealed class JobChangeCommand(
    IJobChangeService jobs,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "jobchange";
    public string Description => "@jobchange <classId> — change your class.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => RunJobChange(jobs, visibility, "@jobchange", caller, args);

    internal static Task RunJobChange(IJobChangeService jobs, IVisibilityService visibility, string label, PlayerEntity caller, IReadOnlyList<string> args)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var classId))
        {
            GmCommandReply.Send(visibility, caller, $"{label}: usage — {label} <classId>");
            return Task.CompletedTask;
        }
        if (jobs.Change(caller, classId))
            GmCommandReply.Send(visibility, caller, $"{label}: now class {classId}.");
        else
            GmCommandReply.Send(visibility, caller, $"{label}: refused (invalid id or same class).");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@job</c> — alias of <c>@jobchange</c>.
/// </summary>
public sealed class JobChangeAliasCommand(
    IJobChangeService jobs,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "job";
    public string Description => "@job <classId> — alias of @jobchange.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => JobChangeCommand.RunJobChange(jobs, visibility, "@job", caller, args);
}
