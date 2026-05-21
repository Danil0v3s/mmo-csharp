using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_FOGWALL — Professor Blinding Mist / Fog Wall. Ground unit
/// placement; per-victim hook applies Blind unless target has
/// SC_DELUGE active.
/// </summary>
public sealed class BlindingMist : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public BlindingMist() : base(SkillIds.PF_FOGWALL) { }
    public BlindingMist(ISkillUnitService? units = null) : base(SkillIds.PF_FOGWALL) => _units = units;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ReferenceEquals(src, target)) return;
        if (ctx.Sc?.Get(target, StatusType.Deluge) != null) return;
        ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
