using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_POISONSMOKE — Poison Smoke. Manual port of
/// <c>rathena-fork/src/map/skills/thief/poisonsmoke.cpp</c>.
/// Drops a poison smoke cell at (x, y); requires SC_POISONINGWEAPON
/// (gating is TODO).
/// </summary>
public sealed class PoisonSmoke : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public PoisonSmoke() : base(SkillIds.GC_POISONSMOKE) { }

    public PoisonSmoke(ISkillUnitService? units = null) : base(SkillIds.GC_POISONSMOKE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
