using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_HOLYCROSS — Crusader Holy Cross. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/holycross.cpp</c>.
///
/// <para>Renewal: ratio <c>+70*lv</c> when wielding a two-handed spear
/// (<c>PlayerEntity.WeaponType == W_2HSPEAR</c>), <c>+35*lv</c>
/// otherwise. 3*lv % chance to apply SC_BLIND on hit.</para>
/// </summary>
public sealed class HolyCross : WeaponSkillImpl
{
    private readonly Random _rng;

    public HolyCross() : base(SkillIds.CR_HOLYCROSS) => _rng = Random.Shared;

    public HolyCross(Random? rng = null) : base(SkillIds.CR_HOLYCROSS)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena W_2HSPEAR = 5 (pc.hpp). Two-handed spear doubles the per-level
        // ratio bump; every other weapon stays at +35*lv.
        const int W_2HSPEAR = 5;
        var bonus = (src is PlayerEntity pc && pc.WeaponType == W_2HSPEAR) ? 70 : 35;
        return baseRatio + bonus * skillLevel;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
