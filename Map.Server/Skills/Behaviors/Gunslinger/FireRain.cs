using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FIRE_RAIN — Rebellion Fire Rain. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/firerain.cpp</c>.
/// Ratio <c>+(-100 + 3500 + 300*lv)</c>. Direction-based wave dispatch
/// + 80 ms-per-wave timers are TODO.
/// </summary>
public sealed class FireRain : SkillImpl
{
    public FireRain() : base(SkillIds.RL_FIRE_RAIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 3500 + 300 * skillLevel);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
