using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_FIRST_BRAND — Inquisitor First Brand. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/firstbrand.cpp</c>.
///
/// <para>Holy splash that brands the victim with
/// <see cref="StatusType.FirstBrand"/>. Brand SCs chain into
/// Second/Third combo skills: the Second-* family looks for
/// FirstBrand on the target as a prerequisite.</para>
///
/// <para>Ratio: <c>-100 + 1200*lv + 5*POW</c>.</para>
/// </summary>
public sealed class FirstBrand : RecursiveDamageSplashSkillImpl
{
    public FirstBrand() : base(SkillIds.IQ_FIRST_BRAND) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 1200 * skillLevel) + 5 * src.Stats.Pow;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start(SC_FIRST_BRAND, 100%, lv, skill_get_time(...))
        // 4000 ms baseline per skill_db.yml Duration1.
        ctx.Sc?.Start(target, StatusType.FirstBrand,
            val1: skillLevel, 0, 0, 0, durationMs: 4000, src);
    }
}
