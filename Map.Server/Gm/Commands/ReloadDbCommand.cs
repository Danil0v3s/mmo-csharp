using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@reloaditemdb / @reloadmobdb / @reloadskilldb</c> — operator-side
/// catalog reloads. rAthena exposes three separate atcommands; we expose
/// them as one command with a subarg so the registration table stays
/// short. Group 99 (admin) only.
/// </summary>
public sealed class ReloadDbCommand(
    IVisibilityService visibility,
    IItemCatalog itemCatalog,
    IMobDb mobDb,
    ISkillDb skillDb
) : IGmCommand
{
    public string Name => "reloaddb";
    public string Description => "@reloaddb <item|mob|skill|all> — re-hydrate the named catalog from the DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var which = args.Count > 0 ? args[0].ToLowerInvariant() : "all";
        switch (which)
        {
            case "item":
                itemCatalog.Reload();
                Ack(caller, $"@reloaddb: item catalog reloaded ({itemCatalog.Count} entries).");
                break;
            case "mob":
                mobDb.Reload();
                Ack(caller, $"@reloaddb: mob_db reloaded ({mobDb.Count} entries).");
                break;
            case "skill":
                skillDb.Reload();
                Ack(caller, $"@reloaddb: skill_db reloaded ({skillDb.Count} entries).");
                break;
            case "all":
                itemCatalog.Reload();
                mobDb.Reload();
                skillDb.Reload();
                Ack(caller, $"@reloaddb: all reloaded ({itemCatalog.Count} items / {mobDb.Count} mobs / {skillDb.Count} skills).");
                break;
            default:
                Ack(caller, "@reloaddb: usage — @reloaddb <item|mob|skill|all>");
                break;
        }
        return Task.CompletedTask;
    }

    private void Ack(PlayerEntity caller, string message)
        => visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = message });
}
