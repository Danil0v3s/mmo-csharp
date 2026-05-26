using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_MADNESS_CRUSHER — Dragon Knight Madness Crusher. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/madnesscrusher.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 1000 + 3800*lv) + 10*POW</c>; doubles when
/// <see cref="StatusType.ChargingpierceCount"/> <c>val1 ≥ 10</c>.</para>
///
/// <para>🚩 INFRA-DEFERRED — rAthena also adds
/// <c>weapon.weight/10 * weapon_level</c> from the right-hand slot.
/// The ratio hook has no session / inventory bridge in this slice.</para>
/// </summary>
public sealed class MadnessCrusher : RecursiveDamageSplashSkillImpl
{
    public MadnessCrusher() : base(SkillIds.DK_MADNESS_CRUSHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 1000 + 3800 * skillLevel) + 10 * src.Stats.Pow;
        var charging = ctx.Sc?.Get(src, StatusType.ChargingpierceCount);
        if (charging != null && charging.Val1 >= 10) ratio *= 2;
        return ratio;
    }
}
