using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_CHUL_HO_SONIC_CLAW — Shaman Chul Ho Sonic Claw. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/chulhosonicclaw.cpp</c>.
/// Ratio <c>+(-100 + 1100 + 2200*lv) + 5*POW</c>; Mystical Creature
/// Mastery / Chul Ho Communion bonuses are TODO.
/// </summary>
public sealed class ChulhoSonicClaw : WeaponSkillImpl
{
    public ChulhoSonicClaw() : base(SkillIds.SH_CHUL_HO_SONIC_CLAW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        int ratio = baseRatio + (-100 + 1100 + 2200 * skillLevel) + 5 * src.Stats.Pow;
        ratio = ShamanFormulas.ApplyMasteryFlat(ratio, src, perLevel: 50, ctx);
        ratio = ShamanFormulas.ApplyCommuneBoost(ratio, src, skillLevel,
            ShamanFormulas.CommuneSpirit.ChulHo,
            flatBase: 0, lvScale: 400, masteryExtra: 50, ctx);
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
    }
}
