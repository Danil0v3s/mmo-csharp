using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_FULLBUSTER — Gunslinger Full Buster. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/fullbuster.cpp</c>.
/// Ratio <c>+100*(lv+2)</c>. After hit, self-blinds at 2*lv%.
/// </summary>
public sealed class FullBuster : WeaponSkillImpl
{
    private readonly Random _rng;

    public FullBuster() : base(SkillIds.GS_FULLBUSTER) => _rng = Random.Shared;

    public FullBuster(Random? rng = null) : base(SkillIds.GS_FULLBUSTER)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel + 2);

    public override void ApplyCounterAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 2 * skillLevel)
            ctx.Sc?.Start(src, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
