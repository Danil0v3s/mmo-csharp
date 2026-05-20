using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Champion;

/// <summary>
/// CH_TIGERFIST — Champion Tiger Knuckle Fist. Mirrors
/// <c>rathena-fork/src/map/skills/champion/tigerfist.cpp</c>.
///
/// Combo skill at (100 + 100*lv)% ATK with chance to inflict stop.
/// Follows the Triple Attack combo chain.
/// </summary>
public sealed class TigerFist : WeaponSkillImpl
{
    public TigerFist() : base(SkillIds.CH_TIGERFIST) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * skillLevel;
}
