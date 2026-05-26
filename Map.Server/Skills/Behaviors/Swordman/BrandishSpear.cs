using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_BRANDISHSPEAR — Knight Brandish Spear. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/brandishspear.cpp</c>.
///
/// <para>Renewal ratio: <c>+(-100 + 400 + 100*lv) + 3*STR</c>.</para>
///
/// <para>🚩 INFRA-DEFERRED — the pre-renewal body uses
/// <c>map_foreachindir</c> for a directional cone splash whose
/// dispatch shape (origin, target direction, depth) doesn't match the
/// radial <see cref="IEntityRegistry.ForEachInRange"/>. We currently
/// land a single hit at the named target via the standard
/// <see cref="WeaponSkillImpl"/> flow. The renewal ratio is parity;
/// the cone splash is only relevant under pre-renewal mode.</para>
/// </summary>
public sealed class BrandishSpear : WeaponSkillImpl
{
    public BrandishSpear() : base(SkillIds.KN_BRANDISHSPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 400 + 100 * skillLevel) + 3 * src.Stats.Str;
}
