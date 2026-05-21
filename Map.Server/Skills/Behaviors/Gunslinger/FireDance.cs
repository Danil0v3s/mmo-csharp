using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FIREDANCE — Rebellion Fire Dance. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/firedance.cpp</c>.
/// Ratio <c>+(100 + 100*lv) + 20*Desperado_lv</c>. Skill-tree bonus is TODO.
/// </summary>
public sealed class FireDance : RecursiveDamageSplashSkillImpl
{
    public FireDance() : base(SkillIds.RL_FIREDANCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 + 100 * skillLevel;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
