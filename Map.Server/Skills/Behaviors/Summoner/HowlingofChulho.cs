using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_HOWLING_OF_CHUL_HO — Shaman Howling of Chul Ho. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/howlingofchulho.cpp</c>.
/// Ratio <c>+(-100 + 600 + 1050*lv) + 5*POW</c>. On hit applies
/// SC_HOGOGONG.
/// </summary>
public sealed class HowlingofChulho : SkillImpl
{
    public HowlingofChulho() : base(SkillIds.SH_HOWLING_OF_CHUL_HO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 600 + 1050 * skillLevel) + 5 * src.Stats.Pow;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Hogogong, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
