using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_PHANTOMTHRUST — Rune Knight Phantom Thrust. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/phantomthrust.cpp</c>.
/// Ratio <c>+(-100 + 50*lv) + 10*SpearMastery_lv</c>, BaseLv/150 modifier.
/// Spear Mastery defaults to 5 here pending skilltree wiring.
/// Pulls target toward caster (knockback) — TODO.
/// </summary>
public sealed class PhantomThrust : WeaponSkillImpl
{
    public PhantomThrust() : base(SkillIds.RK_PHANTOMTHRUST) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        // Deferred: KN_SPEARMASTERY skill id is not yet registered in SkillIds;
        // replace the literal 5 with the learned spear-mastery level once it lands.
        => baseRatio + (-100 + 50 * skillLevel) + 10 * 5;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: skill_blown to pull target toward caster — needs
        // src→target direction resolution from MovementService.
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
