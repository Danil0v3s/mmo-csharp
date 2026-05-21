using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_RAMPAGEBLASTER — Sura Rampage Blaster. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/rampageblaster.cpp</c>.
///
/// <para>Splash damage with Earth Shaker combo bonus. When the
/// target has SC_EARTHSHAKER, ratio jumps to
/// <c>+1400 + 550*lv</c>; otherwise <c>+900 + 350*lv</c>.
/// SC_GT_CHANGE buff multiplies the result by +50 % (TODO —
/// SC reader not on this hook).</para>
/// </summary>
public sealed class RampageBlaster : RecursiveDamageSplashSkillImpl
{
    public RampageBlaster() : base(SkillIds.SR_RAMPAGEBLASTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Default to non-combo path; SC_EARTHSHAKER bonus deferred.
        return baseRatio + 900 + 350 * skillLevel;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        CastendDamageId(src, target, skillLevel, ctx);
    }
}
