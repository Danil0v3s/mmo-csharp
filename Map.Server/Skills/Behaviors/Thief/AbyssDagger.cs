using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_ABYSS_DAGGER — Abyss Dagger. Manual port of
/// <c>rathena-fork/src/map/skills/thief/abyssdagger.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 350 + 1400*lv) + 5*pow</c>.
/// Before the splash detonates the caster latches SC_ABYSS_DAGGER on
/// the target — the buff acts as the partner-token for SC_FATALMENACE's
/// +30*lv ratio bonus on a subsequent cast.
/// </summary>
public sealed class AbyssDagger : RecursiveDamageSplashSkillImpl
{
    public AbyssDagger() : base(SkillIds.ABC_ABYSS_DAGGER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 350 + 1400 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena castendNoDamageId hook fires sc_start before the
        // splash launches. Routing through CastendDamageId here keeps
        // the splash dispatch on the recursive base.
        ctx.Sc?.Start(target, StatusType.AbyssDagger,
            val1: skillLevel, 0, 0, 0, durationMs: 5_000 * skillLevel, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
