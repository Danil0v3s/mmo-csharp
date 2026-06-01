using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_ABYSS_SQUARE — Abyss Square. Manual port of
/// <c>rathena-fork/src/map/skills/thief/abysssquare.cpp</c>.
/// POS2 unit placement; ratio <c>+(-100 + 750*lv) + 5*spl</c> plus
/// <c>+40 * pc_checkskill(ABC_MAGIC_SWORD_M) * lv</c> for PCs that
/// learned the Magic Sword Mastery passive.
/// </summary>
public sealed class AbyssSquare : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public AbyssSquare() : base(SkillIds.ABC_ABYSS_SQUARE) { }

    public AbyssSquare(ISkillUnitService? units = null) : base(SkillIds.ABC_ABYSS_SQUARE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 750 * skillLevel) + 5 * src.Stats.Spl;
        if (src is PlayerEntity pc)
        {
            var swordM = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.ABC_MAGIC_SWORD_M) ?? 0;
            ratio += 40 * swordM * skillLevel;
        }
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
