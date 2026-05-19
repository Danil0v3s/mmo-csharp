using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@pvpon</c> / <c>@pvpoff</c> / <c>@gvgon</c> / <c>@gvgoff</c>
/// — flip the corresponding mapflag for the caller's current map.
/// rAthena <c>atcommand_pvpon</c> / <c>atcommand_pvpoff</c> /
/// <c>atcommand_gvgon</c> / <c>atcommand_gvgoff</c>
/// (atcommand.cpp:2192/2230/2266/2304). The C# IMapFlagService is
/// cache-only today (mapflags come from script); these commands flip
/// the in-memory bit so gates honoring it (combat, skill use, drops)
/// see the change immediately.
/// </summary>
public abstract class MapFlagToggleCommand : IGmCommand
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    protected abstract MapFlag Flag { get; }
    protected abstract bool TurnOn { get; }

    private readonly IVisibilityService _visibility;
    private readonly IMapFlagService _flags;
    private readonly IMapWorldRegistry _maps;

    protected MapFlagToggleCommand(IVisibilityService visibility, IMapFlagService flags, IMapWorldRegistry maps)
    {
        _visibility = visibility;
        _flags = flags;
        _maps = maps;
    }

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == caller.MapId);
        if (map == null) return Task.CompletedTask;
        // IMapFlagService is read-only today; we expose Set via the
        // concrete impl. This is the documented seam — see audit M-H1.
        if (_flags is MapFlagService concrete)
        {
            concrete.Set(map.Name, Flag, TurnOn);
        }
        _visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@{Name}: {Flag} {(TurnOn ? "set" : "cleared")} on {map.Name}.",
        });
        return Task.CompletedTask;
    }
}

public sealed class PvpOnCommand(IVisibilityService v, IMapFlagService f, IMapWorldRegistry m) : MapFlagToggleCommand(v, f, m)
{
    public override string Name => "pvpon";
    public override string Description => "@pvpon — clear nopvp on the current map.";
    protected override MapFlag Flag => MapFlag.NoPvp;
    protected override bool TurnOn => false; // rAthena pvpon = clear NoPvp.
}

public sealed class PvpOffCommand(IVisibilityService v, IMapFlagService f, IMapWorldRegistry m) : MapFlagToggleCommand(v, f, m)
{
    public override string Name => "pvpoff";
    public override string Description => "@pvpoff — set nopvp on the current map.";
    protected override MapFlag Flag => MapFlag.NoPvp;
    protected override bool TurnOn => true;
}

public sealed class GvgOnCommand(IVisibilityService v, IMapFlagService f, IMapWorldRegistry m) : MapFlagToggleCommand(v, f, m)
{
    public override string Name => "gvgon";
    public override string Description => "@gvgon — enable GvG on the current map.";
    protected override MapFlag Flag => MapFlag.Gvg;
    protected override bool TurnOn => true;
}

public sealed class GvgOffCommand(IVisibilityService v, IMapFlagService f, IMapWorldRegistry m) : MapFlagToggleCommand(v, f, m)
{
    public override string Name => "gvgoff";
    public override string Description => "@gvgoff — disable GvG on the current map.";
    protected override MapFlag Flag => MapFlag.Gvg;
    protected override bool TurnOn => false;
}
