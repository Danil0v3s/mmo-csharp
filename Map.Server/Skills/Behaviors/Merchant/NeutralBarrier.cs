using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_NEUTRALBARRIER — Mechanic Neutral Barrier. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/neutralbarrier.cpp</c>.
/// Drops a barrier ground unit + applies SC_NEUTRALBARRIER_MASTER.
/// </summary>
public sealed class NeutralBarrier : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public NeutralBarrier() : base(SkillIds.NC_NEUTRALBARRIER) { }

    public NeutralBarrier(ISkillUnitService? units = null) : base(SkillIds.NC_NEUTRALBARRIER)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _units?.Place(src, SkillId, skillLevel, x, y);
        ctx.Sc?.Start(src, StatusType.NeutralbarrierMaster, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
