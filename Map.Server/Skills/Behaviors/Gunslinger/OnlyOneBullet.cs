using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_ONLY_ONE_BULLET — Night Watch Only One Bullet. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/onlyonebullet.cpp</c>.
/// Ratio <c>+(-100 + 1200 + 3000*lv) + 5*CON</c>. Intensive Aim stacks +
/// revolver bonus are TODO.
/// </summary>
public sealed class OnlyOneBullet : WeaponSkillImpl
{
    public OnlyOneBullet() : base(SkillIds.NW_ONLY_ONE_BULLET) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1200 + 3000 * skillLevel) + 5 * src.Stats.Con;
}
