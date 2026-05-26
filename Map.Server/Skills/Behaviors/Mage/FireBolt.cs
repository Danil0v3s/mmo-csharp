using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_FIREBOLT — Mage Fire Bolt. Mirrors
/// <c>rathena-fork/src/map/skills/mage/firebolt.cpp</c>.
///
/// N-hit single-target Fire magic. Hit count = skill level. Per-hit
/// MATK midpoint × 100 % (skill_db handles element multiplier when
/// the magic resolver runs).
/// </summary>
public sealed class FireBolt : SkillImpl
{
    public FireBolt() : base(SkillIds.MG_FIREBOLT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var perHit = MagicBoltHelper.PerHitDamage(src, ctx.Sc);
        for (var hit = 0; hit < skillLevel; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, src);
        }
    }
}
