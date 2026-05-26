using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_HYUN_ROK_CANNON — Shaman Hyun Rok Cannon. Port of
/// <c>rathena-fork/src/map/skills/summoner/hyunrokcannon.cpp</c>.
///
/// Ratio: <c>-100 + 1100 + 2050·lv + 5·SPL</c>.
/// + Mystical Creature Mastery: <c>+50·mastery</c>.
/// Hyun Rok Communion (either skill learned or SC_TEMPORARY_COMMUNION):
/// <c>+400·lv + 25·mastery</c>.
/// </summary>
public sealed class HyunrokCannon : SkillImpl
{
    public HyunrokCannon() : base(SkillIds.SH_HYUN_ROK_CANNON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int ratio = baseRatio + (-100 + 1100 + 2050 * skillLevel) + 5 * src.Stats.Spl;
        ratio = ShamanFormulas.ApplyMasteryFlat(ratio, src, perLevel: 50, ctx);
        ratio = ShamanFormulas.ApplyCommuneBoost(ratio, src, skillLevel,
            ShamanFormulas.CommuneSpirit.HyunRok,
            flatBase: 0, lvScale: 400, masteryExtra: 25, ctx);
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
