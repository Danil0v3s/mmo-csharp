using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_AM_BLAST — Rebellion Anti-Material Blast. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/antimaterialblast.cpp</c>.
/// Ratio <c>+(-100 + 3500 + 300*lv)</c>. (20 + 10*lv)% to apply
/// SC_ANTI_M_BLAST.
/// </summary>
public sealed class AntiMaterialBlast : WeaponSkillImpl
{
    private readonly Random _rng;

    public AntiMaterialBlast() : base(SkillIds.RL_AM_BLAST) => _rng = Random.Shared;

    public AntiMaterialBlast(Random? rng = null) : base(SkillIds.RL_AM_BLAST)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 3500 + 300 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 20 + 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.AntiMBlast, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
