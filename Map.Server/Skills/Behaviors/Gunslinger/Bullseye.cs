using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_BULLSEYE — Gunslinger Bullseye. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/bullseye.cpp</c>.
/// +400 ratio against non-boss Brute / Demi-Human / Player races; 0.1%
/// coma chance on the same races.
/// </summary>
public sealed class Bullseye : WeaponSkillImpl
{
    private readonly Random _rng;

    public Bullseye() : base(SkillIds.GS_BULLSEYE) => _rng = Random.Shared;

    public Bullseye(Random? rng = null) : base(SkillIds.GS_BULLSEYE)
        => _rng = rng ?? Random.Shared;

    private static bool QualifiesRace(Entity t)
        => t.Stats.Race == BattleRace.Brute
        || t.Stats.Race == BattleRace.Demihuman
        || t.Stats.Race == BattleRace.PlayerHuman
        || t.Stats.Race == BattleRace.PlayerDoram;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        if (QualifiesRace(target) && (target.Stats.Mode & MobMode.StatusImmune) == 0)
            return baseRatio + 400;
        return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (!QualifiesRace(target)) return;
        if (_rng.Next(1000) < 1) // 0.1% chance
            ctx.Sc?.Start(target, StatusType.Coma, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
