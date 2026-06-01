using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ASC_EDP — Enchant Deadly Poison. Manual port of
/// <c>rathena-fork/src/map/skills/thief/enchantdeadlypoison.cpp</c>.
/// Status-only buff that adds +25% poison-element pseudo-watk to the
/// caster via SC_SUB_WEAPONPROPERTY. Val1 carries the property
/// element (rAthena <c>ELE_POISON</c> = 5), Val2 the watk delta
/// (25%), Val3 the granting skill id.
/// </summary>
public sealed class EnchantDeadlyPoison : StatusSkillImpl
{
    /// <summary>rAthena <c>ELE_POISON</c> — element id for poison.</summary>
    private const int ELE_POISON = 5;

    public EnchantDeadlyPoison() : base(SkillIds.ASC_EDP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendNoDamageId(src, target, skillLevel, ctx);

        // rAthena renewal: sc_start4(src, src, SC_SUB_WEAPONPROPERTY,
        //   100, ELE_POISON, 25, getSkillId(), 0, skill_get_time(...)).
        // Duration scales ~60s + 30s per level.
        ctx.Sc?.Start(src, StatusType.SubWeaponproperty,
            val1: ELE_POISON, val2: 25, val3: SkillId, val4: 0,
            durationMs: 60_000 + 30_000 * skillLevel, src);
    }
}
