using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_BANISHING_BUSTER — Rebellion Banishing Buster. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/banishingbuster.cpp</c>.
/// Ratio <c>+(-100 + 1000 + 200*lv)</c>. On hit, dispels up to skillLevel
/// random SCs (50 + 5*lv% success) — dispel logic + SCF_NOBANISHINGBUSTER
/// gating is TODO.
/// </summary>
public sealed class BanishingBuster : WeaponSkillImpl
{
    public BanishingBuster() : base(SkillIds.RL_BANISHING_BUSTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1000 + 200 * skillLevel);
}
