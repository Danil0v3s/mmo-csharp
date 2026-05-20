using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_SOULSTRIKE — Mage Soul Strike. Mirrors
/// <c>rathena-fork/src/map/skills/mage/soulstrike.cpp</c>.
///
/// N-hit Ghost-element magic. Hit count = <c>ceil(lv / 2)</c> —
/// lv1-2 = 1 bolt, lv9-10 = 5 bolts.
/// </summary>
public sealed class SoulStrike : SkillImpl
{
    public SoulStrike() : base(SkillIds.MG_SOULSTRIKE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hitCount = (skillLevel + 1) / 2;
        var perHit = MagicBoltHelper.PerHitDamage(src);
        for (var hit = 0; hit < hitCount; hit++)
        {
            ctx.Damage.ApplyDamage(target, perHit, src);
        }
    }
}
