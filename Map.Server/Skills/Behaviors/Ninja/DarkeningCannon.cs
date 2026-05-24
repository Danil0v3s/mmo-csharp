using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_ANTENPOU — Darkening Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/darkeningcannon.cpp</c>.
///
/// <para>Two-pass splash:
/// <list type="bullet">
///   <item><b>CastendNoDamageId</b> — runs the visual no-damage broadcast and
///         dispatches <c>map_foreachinrange(skill_area_sub … BCT_ENEMY |
///         SD_SPLASH | 1, skill_castend_damage_id)</c>. The
///         <c>skill_mirage_cast</c> ghost-cell unit at the cannon's origin
///         extends the effective range via the SS_SHINKIROU mirror skill.</item>
///   <item><b>CastendDamageId</b> — fires once per splash hit (flag &amp; 1).</item>
///   <item><b>CalculateSkillRatio</b> — baseRatio + (-100 + 450 + 950*lv) +
///         5*SPL; <c>mflag &amp; SKILL_ALTDMG_FLAG</c> downgrades the ratio
///         to 30% (mirror-cast secondary hit).</item>
/// </list></para>
/// </summary>
public sealed class DarkeningCannon : SkillImpl
{
    public DarkeningCannon() : base(SkillIds.SS_ANTENPOU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => CalculateSkillRatio(baseRatio, src, target, skillLevel, ctx: null!, miscflag: 0);

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 450 + 950 * skillLevel);
        ratio += 5 * src.Stats.Spl;
        // rAthena: if (mflag & SKILL_ALTDMG_FLAG) skillratio = skillratio * 3 / 10;
        // Mirror-cast secondary hit is 30% of the primary ratio.
        if ((miscflag & SKILL_ALTDMG_FLAG) != 0)
        {
            ratio = ratio * 3 / 10;
        }
        return ratio;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena darkeningcannon.cpp: castendDamageId only fires on
        // splash hits (flag & 1) — drives a single weapon damage hit
        // per victim through skill_attack.
        ctx.SkillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena: skill_mirage_cast — spawns an SS_SHINKIROU mirror unit
        // at the cannon's origin so the splash can re-fire from the mirror
        // cell. We dispatch the mirror skill at the caster's cell; the
        // SS_SHINKIROU plugin handles the secondary splash. The mirror
        // hit picks up SKILL_ALTDMG_FLAG via the miscflag overload to
        // halve the ratio when the secondary pass fires.
        ctx.Cast?.ResolveSkillAt(src, src.X, src.Y, SkillIds.SS_SHINKIROU, skillLevel);

        // Primary splash — map_foreachinrange(target, BL_CHAR, BCT_ENEMY).
        ctx.SkillAttack?.SkillAttackArea(src, target, SkillId, skillLevel);
    }
}
