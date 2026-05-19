using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@monster &lt;mob_name|id&gt; [amount] [name]</c> — spawn N mobs at
/// the caller's cell. rAthena <c>atcommand_monster</c> (atcommand.cpp:2073).
/// Aliases <c>monstersmall</c> / <c>monsterbig</c> add size modifiers,
/// they route here (we ignore the size flag for now).
/// </summary>
public sealed class MonsterCommand(
    IVisibilityService visibility,
    IMobSpawnService spawn,
    IMobDb mobDb,
    IMapWorldRegistry maps) : IGmCommand
{
    public string Name => "monster";
    public string Description => "@monster <name|id> [amount] — spawn mobs at your cell.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@monster: usage — @monster <name|id> [n]" });
            return Task.CompletedTask;
        }
        var idArg = args[0];
        MobDbEntry? entry = int.TryParse(idArg, out var classId) ? mobDb.Get(classId) : null;
        entry ??= mobDb.GetByAegisName(idArg);
        if (entry == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@monster: '{idArg}' not in mob_db." });
            return Task.CompletedTask;
        }

        var amount = 1;
        if (args.Count >= 2 && int.TryParse(args[1], out var n)) amount = Math.Clamp(n, 1, 100);

        var map = maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == caller.MapId);
        if (map == null) return Task.CompletedTask;

        for (var i = 0; i < amount; i++)
        {
            spawn.SpawnAt(map.Name, entry.Id, caller.X, caller.Y);
        }
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@monster: spawned {amount}× {entry.Name}.",
        });
        return Task.CompletedTask;
    }
}
