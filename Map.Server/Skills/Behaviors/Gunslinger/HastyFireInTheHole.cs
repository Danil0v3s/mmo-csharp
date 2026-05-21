using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_HASTY_FIRE_IN_THE_HOLE — Night Watch Hasty Fire in the Hole.
/// Manual port of <c>rathena-fork/src/map/skills/gunslinger/hastyfireinthehole.cpp</c>.
/// Ratio <c>+(-100 + 1500 + 1500*lv) + 5*CON</c>. Three-stage timed
/// splash with growing radius is TODO.
/// </summary>
public sealed class HastyFireInTheHole : WeaponSkillImpl
{
    public HastyFireInTheHole() : base(SkillIds.NW_HASTY_FIRE_IN_THE_HOLE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1500 + 1500 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
