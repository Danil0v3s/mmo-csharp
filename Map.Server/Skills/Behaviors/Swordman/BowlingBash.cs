using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_BOWLINGBASH — Knight Bowling Bash. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/bowlingbash.cpp</c>.
/// Renewal: ratio <c>+40*lv</c>. With a two-handed sword equipped, multi-hit
/// scales with miscflag (≥4 → 4 hits, ≥2 → 3 hits). The chain/blow gutter
/// logic (pre-renewal) is not ported — TODO once we have map_foreachindir
/// and the bowling_db tracking.
/// </summary>
public sealed class BowlingBash : WeaponSkillImpl
{
    public BowlingBash() : base(SkillIds.KN_BOWLINGBASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 40 * skillLevel;

    // Deferred: ModifyDamageData should bump Hits to 3 or 4 with a 2H sword
    // (W_2HSWORD = 3) based on the splash miscflag (≥2 → 3 hits, ≥4 → 4 hits).
    // The miscflag count isn't yet plumbed into the damage-modify hook, so the
    // multi-hit branch waits on that wiring.
}
