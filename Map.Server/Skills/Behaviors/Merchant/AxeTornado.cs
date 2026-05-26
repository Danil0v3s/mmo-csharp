using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_AXETORNADO — Mechanic Axe Tornado. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/axetornado.cpp</c>.
/// Ratio: <c>+(-100 + 200 + 180*lv) + 2*VIT</c>; when the caster has
/// SC_AXE_STOMP active, adds an additional +380.
/// </summary>
public sealed class AxeTornado : RecursiveDamageSplashSkillImpl
{
    public AxeTornado() : base(SkillIds.NC_AXETORNADO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 200 + 180 * skillLevel) + src.Stats.Vit * 2;
        if (ctx.Sc?.Get(src, StatusType.AxeStomp) != null)
            ratio += 380;
        return ratio;
    }
}
