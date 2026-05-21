using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_INTIMIDATE — Snatch. Manual port of
/// <c>rathena-fork/src/map/skills/thief/snatch.cpp</c>.
/// +30*lv ratio. Original hits + teleports the target; teleport-on-hit
/// behaviour is TODO.
/// </summary>
public sealed class Snatch : WeaponSkillImpl
{
    public Snatch() : base(SkillIds.RG_INTIMIDATE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel;
}
