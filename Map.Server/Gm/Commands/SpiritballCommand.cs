using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@spiritball &lt;count&gt;</c> — grant N Monk/Sura spirit balls.
/// rAthena <c>atcommand_spiritball</c> (atcommand.cpp:2467). Mostly a
/// debug command — production grants come from skills (Summon Spirit
/// Sphere). Aliases <c>@soulball</c> route via atcommands.yml.
/// </summary>
public sealed class SpiritballCommand(
    IPlayerOrbService orbs,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "spiritball";
    public string Description => "@spiritball <count> — add spirit balls (capped at 15).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var count = 1;
        if (args.Count > 0 && int.TryParse(args[0], out var n)) count = Math.Clamp(n, 0, 15);
        orbs.Add(caller, OrbKind.Spirit, count);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@spiritball: +{count} → {orbs.Get(caller, OrbKind.Spirit)}.",
        });
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@soulball &lt;count&gt;</c> — grant N Soul Reaper souls.
/// rAthena <c>atcommand_soulball</c> (atcommand.cpp:2487).
/// </summary>
public sealed class SoulballCommand(
    IPlayerOrbService orbs,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "soulball";
    public string Description => "@soulball <count> — add soul balls (capped at 20).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var count = 1;
        if (args.Count > 0 && int.TryParse(args[0], out var n)) count = Math.Clamp(n, 0, 20);
        orbs.Add(caller, OrbKind.Soul, count);
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@soulball: +{count} → {orbs.Get(caller, OrbKind.Soul)}.",
        });
        return Task.CompletedTask;
    }
}
