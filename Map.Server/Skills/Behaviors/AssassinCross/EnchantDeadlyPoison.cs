using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.AssassinCross;

/// <summary>
/// ASC_EDP — Assassin Cross Enchant Deadly Poison. Mirrors
/// <c>rathena-fork/src/map/skills/assassincross/enchantdeadlypoison.cpp</c>.
///
/// Apply <see cref="StatusType.Edp"/> on caster — weapon attacks
/// deal massively boosted Poison damage. Val1 = level. Duration
/// <c>30 + 30*lv</c> seconds.
/// </summary>
public sealed class EnchantDeadlyPoison : SkillImpl
{
    public EnchantDeadlyPoison() : base(SkillIds.ASC_EDP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Edp, val1: skillLevel, 0, 0, 0,
            durationMs: (30 + 30 * skillLevel) * 1000, src);
    }
}
