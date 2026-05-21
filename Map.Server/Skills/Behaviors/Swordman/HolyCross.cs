using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_HOLYCROSS — Crusader Holy Cross. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/holycross.cpp</c>.
/// Renewal: ratio <c>+70*lv</c> with a 2H spear, <c>+35*lv</c> otherwise.
/// 3*lv % chance to apply SC_BLIND. Weapon-type query is TODO; we use
/// the lower-ratio default.
/// </summary>
public sealed class HolyCross : WeaponSkillImpl
{
    private readonly Random _rng;

    public HolyCross() : base(SkillIds.CR_HOLYCROSS) => _rng = Random.Shared;

    public HolyCross(Random? rng = null) : base(SkillIds.CR_HOLYCROSS)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 35 * skillLevel;
    // TODO: + 70*lv with W_2HSPEAR.

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
