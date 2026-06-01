using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_INTIMIDATE — Snatch. Manual port of
/// <c>rathena-fork/src/map/skills/thief/snatch.cpp</c>.
/// Ratio <c>+30*lv</c>. rAthena keeps the warp-on-hit teleport in the
/// engine-side post-damage hook (skill.cpp's RG_INTIMIDATE arm fires
/// <c>pc_setpos(SavePoint)</c> when the target dies); the .cpp body
/// itself is just the ratio formula, so this port is exact.
/// </summary>
public sealed class Snatch : WeaponSkillImpl
{
    public Snatch() : base(SkillIds.RG_INTIMIDATE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel;
}
