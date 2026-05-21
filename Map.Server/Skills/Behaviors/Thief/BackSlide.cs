using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_BACKSLIDING — Back Slide. Manual port of
/// <c>rathena-fork/src/map/skills/thief/backslide.cpp</c>.
/// Knocks the caster backwards (BLOWN_IGNORE_NO_KNOCKBACK) and grants
/// 200ms unstoppable. Endure-tick grant is TODO; this stub does the
/// knockback animation only.
/// </summary>
public sealed class BackSlide : SkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    public BackSlide() : base(SkillIds.TF_BACKSLIDING) { }

    public BackSlide(IUnitOpsService? unitOps = null) : base(SkillIds.TF_BACKSLIDING)
    {
        _unitOps = unitOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Blow backward from the caster's facing direction (-blewcount cells).
        _unitOps?.BlownBy(target, 0 /* TODO: opposite-of-target dir */, 5);
    }
}
