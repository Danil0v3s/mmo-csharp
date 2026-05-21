using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_PIERCINGSHOT — Gunslinger Piercing Shot. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/piercingshot.cpp</c>.
/// Renewal ratio <c>+(100 + 20*lv)</c>; rifle bonus +150+30*lv (TODO).
/// 3*lv% bleed on hit.
/// </summary>
public sealed class PiercingShot : WeaponSkillImpl
{
    private readonly Random _rng;

    public PiercingShot() : base(SkillIds.GS_PIERCINGSHOT) => _rng = Random.Shared;

    public PiercingShot(Random? rng = null) : base(SkillIds.GS_PIERCINGSHOT)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 + 20 * skillLevel;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }
}
