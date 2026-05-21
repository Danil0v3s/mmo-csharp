using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_HERMODE — Clown/Gypsy Wand of Hermode. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wandofhermode.cpp</c>.
/// Drops the song unit (renewal hands off to skill_castend_song —
/// dispatcher TODO).
/// </summary>
public sealed class WandOfHermode : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public WandOfHermode() : base(SkillIds.CG_HERMODE) { }

    public WandOfHermode(ISkillUnitService? units = null) : base(SkillIds.CG_HERMODE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
