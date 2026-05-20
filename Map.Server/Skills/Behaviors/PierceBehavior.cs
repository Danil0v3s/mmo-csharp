using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// KN_PIERCE (id 56) — Knight Pierce. rAthena
/// <c>skill.cpp:case KN_PIERCE</c>: physical multi-hit, hit count =
/// target Size + 1 (Small=1, Medium=2, Large=3). Each hit deals
/// (100 + 10 * lv)% ATK.
///
/// Requires spear weapon — gated upstream at requirement check.
/// </summary>
public sealed class PierceBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.KN_PIERCE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
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
            var swing = ctx.Battle.CalcWeaponAttack(source, target);
            var dmg = (int)Math.Clamp(swing.Total * rate / 100, 0, int.MaxValue);
            ctx.Damage.ApplyDamage(target, dmg, source);
        }
        return true;
    }
}
