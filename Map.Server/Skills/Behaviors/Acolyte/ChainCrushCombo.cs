using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CH_CHAINCRUSH — Champion Chain Crush Combo. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/chaincrushcombo.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c>; GT_ENERGYGAIN adds +50 % when active.
/// </summary>
public sealed class ChainCrushCombo : WeaponSkillImpl
{
    public ChainCrushCombo() : base(SkillIds.CH_CHAINCRUSH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Renewal: skillratio += -100 + 200 * skill_lv;  RE_LVL_DMOD(100);
        // Final: (-100 + 200*lv) + base 100 = 200*lv % at base level.
        return baseRatio + (-100 + 200 * skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // GT_ENERGYGAIN bumps damage by +50% multiplicatively (rAthena bumps
        // the skillratio inside calc_misc; we apply on top of the resolved
        // damage so the base ratio formula above stays simple).
        // COMBAT-96 — skill-aware swing (crit_atk_rate ÷100 suppressed) + the ÷200 skill bump below.
        var swing = ctx.Battle.CalcWeaponAttack(src, target, SkillId);
        var ratio = CalculateSkillRatio(100, src, target, skillLevel);
        var dmg = (long)swing.Total * ratio / 100;
        if (ctx.Sc?.Get(src, StatusType.GtEnergygain) != null)
        {
            dmg = dmg * 150 / 100;
        }
        // COMBAT-96 — crit_atk_rate ÷200 (battle.cpp:7787), after the full ratio (incl. GtEnergygain).
        dmg = ApplySkillCritAtkRate(dmg, src, swing);
        ctx.Damage.ApplyDamage(target, (int)Math.Clamp(dmg, 0, int.MaxValue), src);
        ApplyAdditionalEffects(src, target, skillLevel, ctx);
    }
}
