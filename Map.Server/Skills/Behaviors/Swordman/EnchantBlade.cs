using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_ENCHANTBLADE — Rune Knight Enchant Blade. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/enchantblade.cpp</c>.
/// Val1 = lv, Val2 = <c>((100 + 20*lv) * BaseLv / 100) + INT</c>.
/// </summary>
public sealed class EnchantBlade : SkillImpl
{
    public EnchantBlade() : base(SkillIds.RK_ENCHANTBLADE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var atkBonus = ((100 + 20 * skillLevel) * src.Level) / 100 + src.Stats.IntStat;
        ctx.Sc?.Start(target, StatusType.Enchantblade, val1: skillLevel, val2: atkBonus, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
