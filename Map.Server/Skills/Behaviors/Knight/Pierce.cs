using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Knight;

/// <summary>
/// KN_PIERCE — Knight Pierce. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/pierce.cpp</c>.
///
/// Multi-hit weapon attack: hit count = target Size + 1
/// (Small=1, Medium=2, Large=3). Each hit at (100 + 10*lv)% ATK.
///
/// Returns hits through repeated calls to the standard swing path
/// rather than overriding <see cref="WeaponSkillImpl.CastendDamageId"/>,
/// since the per-hit cardfix / element fix should run individually.
/// </summary>
public sealed class Pierce : SkillImpl
{
    public Pierce() : base(SkillIds.KN_PIERCE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hitCount = target.Stats.Size switch
        {
            BattleSize.Small => 1,
            BattleSize.Medium => 2,
            BattleSize.Large => 3,
            _ => 1,
        };
        var rate = 100 + 10 * skillLevel;
        for (var hit = 0; hit < hitCount; hit++)
        {
            var swing = ctx.Battle.CalcWeaponAttack(src, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, src);
        }
    }
}
