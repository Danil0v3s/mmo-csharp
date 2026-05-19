using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@speed &lt;ms&gt;</c> — set per-cell walk delay in milliseconds.
/// rAthena <c>atcommand_speed</c> (atcommand.cpp:5089). 150 is the PC
/// baseline; 1 = blink-step; -1 resets to baseline. rAthena clamps
/// 0..MAX_WALK_SPEED (1000); we mirror that.
/// </summary>
public sealed class SpeedCommand(
    IVisibilityService visibility,
    ISessionManagerAccessor sessions) : IGmCommand
{
    public string Name => "speed";
    public string Description => "@speed <ms> — set walk speed (150 = default, 1 = max, -1 = reset).";

    private const int Default = 150;
    private const int Max = 1000;

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var ms))
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = "@speed: usage — @speed <ms>" });
            return Task.CompletedTask;
        }
        var target = ms < 0 ? Default : Math.Clamp(ms, 1, Max);
        caller.Speed = target;
        caller.Stats.Speed = (ushort)target;

        var s = sessions.GetByEntityId(caller.Id);
        s?.EnqueuePacket(new ZC_PAR_CHANGE { VarId = SpId.SP_SPEED, Value = target });
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = $"@speed: set to {target}." });
        return Task.CompletedTask;
    }
}
