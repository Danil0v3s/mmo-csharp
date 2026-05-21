using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_THE_ONE_FIGHTER_RISES — Homunculus The One Fighter Rises.
/// Manual port of <c>rathena-fork/src/map/skills/homunculus/homunculus_theonefighterrises.cpp</c>.
/// Ratio <c>+(-100 + 580*lv*BaseLv/100) + STR</c>. On cast grants
/// MAX_SPIRITBALL stacks (TODO).
/// </summary>
public sealed class TheOneFighterRises : RecursiveDamageSplashSkillImpl
{
    public TheOneFighterRises() : base(SkillIds.MH_THE_ONE_FIGHTER_RISES) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 580 * skillLevel * src.Level / 100) + src.Stats.Str;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
