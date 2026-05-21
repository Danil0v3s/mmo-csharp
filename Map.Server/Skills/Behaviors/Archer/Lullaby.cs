using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_LULLABY — Bard Lullaby. Manual port of
/// <c>rathena-fork/src/map/skills/archer/lullaby.cpp</c>.
/// Renewal: per-tick splash victims have 100 % chance of SC_SLEEP.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class Lullaby : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Lullaby() : base(SkillIds.BD_LULLABY) { }

    public Lullaby(ISkillUnitService? units = null) : base(SkillIds.BD_LULLABY)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Sleep, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
