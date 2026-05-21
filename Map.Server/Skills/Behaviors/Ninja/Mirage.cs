using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_SHINKIROU — Mirage. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/mirage.cpp</c>.
/// Self-buff + cell unit placement at target cell.
/// </summary>
public sealed class Mirage : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Mirage() : base(SkillIds.SS_SHINKIROU) { }

    public Mirage(ISkillUnitService? units = null) : base(SkillIds.SS_SHINKIROU)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // TODO: self SC_SHINKIROU.
        _units?.Place(src, SkillId, skillLevel, x, y);
    }
}
