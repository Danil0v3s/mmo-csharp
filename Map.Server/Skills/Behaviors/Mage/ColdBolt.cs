using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_COLDBOLT — Mage Cold Bolt. Mirrors
/// <c>rathena-fork/src/map/skills/mage/coldbolt.cpp</c>.
/// N-hit single-target Water magic, same shape as Fire Bolt.
/// </summary>
public sealed class ColdBolt : SkillImpl
{
    public ColdBolt() : base(SkillIds.MG_COLDBOLT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var perHit = MagicBoltHelper.PerHitDamage(src, ctx.Sc);
        for (var hit = 0; hit < skillLevel; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, src);
        }
    }
}
