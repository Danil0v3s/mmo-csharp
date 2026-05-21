using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_SPRINGTRAP — Hunter Spring Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/springtrap.cpp</c>.
///
/// <para>Manually trips a previously-laid trap (Ankle Snare / Blast
/// Mine / Skid Trap / Land Mine / Shockwave / Sandman / Flasher /
/// Freezing Trap / Claymore Trap / Talkie Box). Operates on the
/// trap's skill_unit group — that lookup isn't surfaced to the
/// behavior hook here, so we land the broadcast frame only.</para>
/// </summary>
public sealed class SpringTrap : SkillImpl
{
    public SpringTrap() : base(SkillIds.HT_SPRINGTRAP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: trip the targeted skill_unit when ground-unit references land in ctx.
    }
}
