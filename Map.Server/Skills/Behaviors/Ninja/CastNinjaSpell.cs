using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_ZENKAI — Cast Ninja Spell. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/castninjaspell.cpp</c>.
/// Drops the spell-charm cell unit at the targeted cell.
/// </summary>
public sealed class CastNinjaSpell : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CastNinjaSpell() : base(SkillIds.KO_ZENKAI) { }

    public CastNinjaSpell(ISkillUnitService? units = null) : base(SkillIds.KO_ZENKAI)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
