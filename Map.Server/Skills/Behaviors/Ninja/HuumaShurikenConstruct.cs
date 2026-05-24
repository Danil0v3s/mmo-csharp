using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_FUUMAKOUCHIKU — Huuma Shuriken Construct
/// (rathena-fork/src/map/skills/ninja/huumashurikenconstruct.cpp).
/// Ratio: <c>baseRatio + (-100 + 900 + 1750*lv) + 5*POW +
/// pc_checkskill(SS_FUUMASHOUAKU) * 100 * lv</c>. The
/// <c>SKILL_ALTDMG_FLAG</c> branch (+200) fires on the path-AoE
/// secondary hit pass through <c>skill_attack_area</c>.
///
/// <para>rAthena dispatches this skill via <c>castendPos2</c> as a
/// directional path-AoE (<c>map_foreachinpath</c> on BL_CHAR | BL_SKILL).
/// Each victim takes the standard weapon hit; ground-unit hits
/// (BL_SKILL) carry SKILL_ALTDMG_FLAG so the ratio bumps +200.
/// We approximate the path-AoE by routing through the standard
/// splash radius (skill_db) — the path-shaped iteration ports when
/// <c>map_foreachinpath</c> lands.</para>
/// </summary>
public sealed class HuumaShurikenConstruct : WeaponSkillImpl
{
    public HuumaShurikenConstruct() : base(SkillIds.SS_FUUMAKOUCHIKU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => CalculateSkillRatio(baseRatio, src, target, skillLevel, ctx: null!, miscflag: 0);

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 900 + 1750 * skillLevel);
        // rAthena: skillratio += 200 when wd->miscflag & SKILL_ALTDMG_FLAG.
        // The alt-dmg pass fires for BL_SKILL hits on the path-AoE; the
        // miscflag-aware dispatcher in CastendPos2 below sets the bit.
        if ((miscflag & SKILL_ALTDMG_FLAG) != 0)
        {
            ratio += 200;
        }
        ratio += 5 * src.Stats.Pow;
        if (src is PlayerEntity pc)
        {
            var fuumaLv = pc.LearnedSkills.GetValueOrDefault(SkillIds.SS_FUUMASHOUAKU);
            if (fuumaLv > 0) ratio += fuumaLv * 100 * skillLevel;
        }
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena castendPos2 — path-AoE splash on BL_CHAR | BL_SKILL.
        // The path-shaped iteration (map_foreachinpath / map_foreachindir)
        // isn't ported yet, so we fall back to the standard square splash
        // around the ground cell. Each char victim gets the standard
        // weapon hit; the BL_SKILL alt-flag (+200 ratio) lands when
        // ground-unit damage routing ports — see CalculateSkillRatio
        // above, which already honors SKILL_ALTDMG_FLAG when the caller
        // passes it through.
        const short splash = 2;
        var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, splash,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            CastendDamageId(src, v, skillLevel, ctx, miscflag: 0);
        }
    }
}
