using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_WILD_FIRE — Night Watch Wild Fire. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/wildfire.cpp</c>.
/// Ratio <c>+(-100 + 1500 + 3000*lv) + 5*CON</c>. Intensive Aim / shotgun
/// bonuses + splash dispatch are TODO.
/// </summary>
public sealed class WildFire : WeaponSkillImpl
{
    public WildFire() : base(SkillIds.NW_WILD_FIRE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1500 + 3000 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
