using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_BANDING — Royal Guard Banding. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/banding.cpp</c>.
/// Toggles SC_BANDING; drops a unit group at the caster's cell when
/// starting the effect.
/// </summary>
public sealed class Banding : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Banding() : base(SkillIds.LG_BANDING) { }

    public Banding(ISkillUnitService? units = null) : base(SkillIds.LG_BANDING)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null && ctx.Sc.Get(src, StatusType.Banding) != null)
        {
            ctx.Sc.End(src, StatusType.Banding);
        }
        else
        {
            _units?.Place(src, SkillId, skillLevel, src.X, src.Y);
            ctx.Sc?.Start(src, StatusType.Banding, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
