using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_LIGHTNINGBOLT — Mage Lightning Bolt. Mirrors
/// <c>rathena-fork/src/map/skills/mage/lightningbolt.cpp</c>.
/// N-hit single-target Wind magic, same shape as Fire Bolt.
/// </summary>
public sealed class LightningBolt : SkillImpl
{
    public LightningBolt() : base(SkillIds.MG_LIGHTNINGBOLT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var perHit = MagicBoltHelper.PerHitDamage(src, ctx.Sc);
        for (var hit = 0; hit < skillLevel; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, src);
        }
    }
}
