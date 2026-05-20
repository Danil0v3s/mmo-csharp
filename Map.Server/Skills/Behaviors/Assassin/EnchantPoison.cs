using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Assassin;

/// <summary>
/// AS_ENCHANTPOISON — Assassin Enchant Poison. Mirrors
/// <c>rathena-fork/src/map/skills/assassin/enchantpoison.cpp</c>.
///
/// Apply <see cref="StatusType.Encpoison"/> on target — weapon
/// becomes Poison element + small Poison-proc chance per hit.
/// Duration <c>60 + 60*lv</c>s.
/// </summary>
public sealed class EnchantPoison : SkillImpl
{
    public EnchantPoison() : base(SkillIds.AS_ENCHANTPOISON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Encpoison, val1: skillLevel, 0, 0, 0,
            durationMs: 60_000 + 60_000 * skillLevel, src);
    }
}
