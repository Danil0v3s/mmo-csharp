using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@mount</c> — toggle Peco Peco riding. rAthena
/// <c>atcommand_mount_peco</c> (atcommand.cpp:5588) flips
/// <c>OPTION_RIDING</c>. Mado / Dragon variants ship as separate
/// commands (@madogear, @dragon) and route through the same option
/// service.
/// </summary>
public sealed class MountCommand(
    IPlayerOptionService options,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "mount";
    public string Description => "@mount — toggle Peco Peco riding.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var on = (caller.Option & PlayerOption.Riding) == 0;
        options.SetRiding(caller, on);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = on ? "@mount: mounted." : "@mount: dismounted.",
        });
        return Task.CompletedTask;
    }
}
