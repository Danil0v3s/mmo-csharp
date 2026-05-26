using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_MIGHTY_SMASH — Meister Mighty Smash. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/mightysmash.cpp</c>.
/// Ratio: <c>+(-100 + 80 + 240*lv) + 5*POW</c>; when the caster has
/// SC_AXE_STOMP active, adds an additional <c>+20 +5*POW</c>.
///
/// <para>SC_AXE_STOMP also splits the hit into 7 div_ — 🚩 INFRA-DEFERRED
/// (ModifyDamageData lacks SC access; reroute when the hook gains a
/// ctx parameter).</para>
/// </summary>
public sealed class MightySmash : RecursiveDamageSplashSkillImpl
{
    public MightySmash() : base(SkillIds.MT_MIGHTY_SMASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 80 + 240 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.AxeStomp) != null)
        {
            ratio += 20;
            ratio += 5 * src.Stats.Pow;
        }
        return ratio;
    }
}
