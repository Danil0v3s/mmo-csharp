using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_FLASHER — Hunter Flasher trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/flasher.cpp</c>.
/// Trap + on-hit SC_BLIND at 100 %.
/// </summary>
public sealed class Flasher : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Flasher() : base(SkillIds.HT_FLASHER) { }

    public Flasher(ISkillUnitService? units = null) : base(SkillIds.HT_FLASHER)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
