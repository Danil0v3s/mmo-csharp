using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@hide</c> — toggle GM invisibility. rAthena <c>atcommand_hide</c>
/// (atcommand.cpp:1432) — flips the OPTION_INVISIBLE flag and broadcasts
/// vanish/standentry. We replay the same wire pattern: vanish to AOI when
/// hiding, standentry when revealing.
/// </summary>
public sealed class HideCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "hide";
    public string Description => "@hide — toggle GM invisibility to nearby players.";

    /// <summary>Tracks the hidden character ids — kept here so toggling without persistence works in-process.</summary>
    private static readonly HashSet<int> Hidden = new();

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (Hidden.Remove(caller.CharacterId))
        {
            visibility.NotifySpawnedToArea(caller);
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@hide: visible." });
        }
        else
        {
            Hidden.Add(caller.CharacterId);
            visibility.NotifyVanishedToArea(caller, VanishReason.Outsight);
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@hide: invisible." });
        }
        return Task.CompletedTask;
    }
}
