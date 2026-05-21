using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_SERVANT_W_PHANTOM — Dragon Knight Servant Weapon: Phantom.
/// Manual port of <c>rathena-fork/src/map/skills/swordman/servantweaponphantom.cpp</c>.
/// Ratio <c>+(-100 + 200 + 300*lv) + 5*POW</c>. On hit, <c>30 + 10*lv</c>%
/// chance to apply SC_HANDICAPSTATE_DEEPBLIND.
/// </summary>
public sealed class ServantWeaponPhantom : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;

    public ServantWeaponPhantom() : base(SkillIds.DK_SERVANT_W_PHANTOM) => _rng = Random.Shared;

    public ServantWeaponPhantom(Random? rng = null) : base(SkillIds.DK_SERVANT_W_PHANTOM)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 + 300 * skillLevel) + 5 * src.Stats.Pow;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 30 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.HandicapstateDeepblind, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
