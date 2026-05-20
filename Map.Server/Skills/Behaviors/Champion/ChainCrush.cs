using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Champion;

/// <summary>
/// CH_CHAINCRUSH — Champion Chain Crush. Mirrors
/// <c>rathena-fork/src/map/skills/champion/chaincrush.cpp</c>.
///
/// Multi-hit combo skill that ends the Tiger Knuckle Fist chain.
/// Per-hit (200 + 50*lv)% ATK, hit count = 3 + lv/2.
/// </summary>
public sealed class ChainCrush : SkillImpl
{
    public ChainCrush() : base(SkillIds.CH_CHAINCRUSH) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hitCount = 3 + skillLevel / 2;
        var rate = 200 + 50 * skillLevel;
        for (var hit = 0; hit < hitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
