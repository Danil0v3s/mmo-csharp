using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_EXPLOSIVE_POWDER — Biolo Explosive Powder. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/explosivepowder.cpp</c>.
/// Ratio: <c>+(-100 + 500 + 650*lv) + 5*POW</c>; when the caster has
/// SC_RESEARCHREPORT active, adds <c>+100*lv</c>.
///
/// <para>SC_RESEARCHREPORT also forces div_ = 5 — 🚩 INFRA-DEFERRED
/// (ModifyDamageData lacks SC access; reroute when the hook gains a
/// ctx parameter).</para>
/// </summary>
public sealed class ExplosivePowder : RecursiveDamageSplashSkillImpl
{
    public ExplosivePowder() : base(SkillIds.BO_EXPLOSIVE_POWDER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 500 + 650 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.Researchreport) != null)
            ratio += 100 * skillLevel;
        return ratio;
    }
}
