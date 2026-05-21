using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_DRAGONCOMBO — Sura Dragon Combo. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/dragoncombo.cpp</c>.
/// Ratio <c>+(100 + 80*lv)</c>; (1+lv)% stun on hit.
/// </summary>
public sealed class DragonCombo : WeaponSkillImpl
{
    public DragonCombo() : base(SkillIds.SR_DRAGONCOMBO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += 100 + 80 * skill_lv;  RE_LVL_DMOD(100);
        return baseRatio + 100 + 80 * skillLevel;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start(src, target, SC_STUN, 1 + skill_lv, skill_lv, skill_get_time(...));
        // Stun chance = 1 + lv % (very small but not zero); duration from skill_db.
        if (Random.Shared.Next(100) < 1 + skillLevel)
        {
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel,
                0, 0, 0, durationMs: 2000, src);
        }
    }
}
