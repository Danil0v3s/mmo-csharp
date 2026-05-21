using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_HOGOGONG_STRIKE — Shaman Hogogong Strike. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/hogogongstrike.cpp</c>.
/// Ratio <c>+(-100 + 180 + 200*lv) + 5*POW</c>. Mystical Creature
/// Mastery + Chul Ho Communion bonuses are TODO; on-hit damage only
/// lands when target has SC_HOGOGONG (TODO).
/// </summary>
public sealed class HogogongStrike : SkillImpl
{
    public HogogongStrike() : base(SkillIds.SH_HOGOGONG_STRIKE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 180 + 200 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
