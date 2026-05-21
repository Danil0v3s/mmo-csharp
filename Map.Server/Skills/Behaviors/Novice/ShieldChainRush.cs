using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// HN_SHIELD_CHAIN_RUSH — Hyper Novice Shield Chain Rush. Manual port
/// of <c>rathena-fork/src/map/skills/novice/shieldchainrush.cpp</c>.
/// Ratio <c>+(-100 + 850 + 1050*lv) + 5*POW</c>. On cast applies
/// SC_HNNOWEAPON to the caster. HN_SELFSTUDY_TATICS bonus is TODO.
/// </summary>
public sealed class ShieldChainRush : WeaponSkillImpl
{
    public ShieldChainRush() : base(SkillIds.HN_SHIELD_CHAIN_RUSH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 850 + 1050 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.Start(src, StatusType.Hnnoweapon, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
