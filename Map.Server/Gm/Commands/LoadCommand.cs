using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@load</c> / <c>@return</c> — warp the caller to their savepoint.
/// rAthena <c>atcommand_load</c> (atcommand.cpp:5077). Goes through the
/// same pipe as savepoint-on-death — Respawn — to keep one code path.
/// </summary>
public sealed class LoadCommand(
    IVisibilityService visibility,
    IPcDeathService death) : IGmCommand
{
    public string Name => "load";
    public string Description => "@load — warp to your savepoint.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // Respawn uses the savepoint stored via SetSavepoint; rAthena's
        // @load calls pc_setpos with the same coords without the death
        // gate. We force the warp here even when the caller is alive by
        // re-using the savepoint path.
        if (!death.WarpToSavepoint(caller))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@load: no savepoint stored." });
        }
        return Task.CompletedTask;
    }
}
