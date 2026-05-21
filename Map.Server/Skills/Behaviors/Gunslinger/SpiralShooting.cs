using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_SPIRAL_SHOOTING — Night Watch Spiral Shooting. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/spiralshooting.cpp</c>.
/// Ratio <c>+(-100 + 1200 + 1700*lv) + 5*CON</c>. Intensive Aim / rifle
/// bonuses are TODO; splash dispatch is TODO.
/// </summary>
public sealed class SpiralShooting : SkillImpl
{
    public SpiralShooting() : base(SkillIds.NW_SPIRAL_SHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1200 + 1700 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
