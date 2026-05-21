using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_HYUN_ROK_CANNON — Shaman Hyun Rok Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/hyunrokcannon.cpp</c>.
/// Ratio <c>+(-100 + 1100 + 2050*lv) + 5*SPL</c>. Mystical Creature
/// Mastery + Hyun Rok Communion bonuses are TODO.
/// </summary>
public sealed class HyunrokCannon : SkillImpl
{
    public HyunrokCannon() : base(SkillIds.SH_HYUN_ROK_CANNON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1100 + 2050 * skillLevel) + 5 * src.Stats.Spl;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
