using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_GENTLETOUCH_QUIET — Sura Gentle Touch: Quiet. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/gentletouchquiet.cpp</c>.
/// Ratio <c>+(-100 + 100*lv) + DEX</c>; silences with rate
/// <c>5*lv + (DEX + BaseLv) / 10</c>%.
/// </summary>
public sealed class GentleTouchQuiet : WeaponSkillImpl
{
    public GentleTouchQuiet() : base(SkillIds.SR_GENTLETOUCH_QUIET) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: chance = 5*lv + (caster.dex + caster.lv) / 10
        // sc_start(SC_SILENCE, chance%, skill_lv, skill_get_time(...))
        var chance = 5 * skillLevel + (src.Stats.Dex + src.Level) / 10;
        if (Random.Shared.Next(100) < chance)
        {
            ctx.Sc?.Start(target, StatusType.Silence, val1: skillLevel,
                0, 0, 0, durationMs: 5000, src);
        }
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 100*skill_lv + caster.dex;
        // RE_LVL_DMOD(100) applied later by damage formula.
        return baseRatio + (-100 + 100 * skillLevel) + src.Stats.Dex;
    }
}
