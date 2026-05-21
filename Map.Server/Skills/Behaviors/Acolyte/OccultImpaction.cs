using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_INVESTIGATE — Monk Occult Impaction. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/occultimpaction.cpp</c>.
///
/// <para>Standard weapon hit. Renewal ratio: <c>-100 + 100*lv</c>
/// (+50 % when target is Blade Stop'd, since the skill consumes
/// the BladeStop catch). Ends <c>SC_BLADESTOP</c> on the caster
/// after the hit lands — Blade Stop is the prerequisite combo state.</para>
/// </summary>
public sealed class OccultImpaction : WeaponSkillImpl
{
    public OccultImpaction() : base(SkillIds.MO_INVESTIGATE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: WeaponSkillImpl::castendDamageId(...); status_change_end(src, SC_BLADESTOP);
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.End(src, StatusType.Bladestop);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Renewal: skillratio += -100 + 100 * skill_lv.
        // SC_BLADESTOP on target → +50 % multiplicative (combo bonus).
        // Without SC reader in CalculateSkillRatio hook, the combo
        // bonus is deferred (TODO).
        return baseRatio + (-100 + 100 * skillLevel);
    }
}
