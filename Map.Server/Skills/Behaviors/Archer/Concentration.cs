using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_CONCENTRATION — Archer Improve Concentration. Manual port of
/// <c>rathena-fork/src/map/skills/archer/concentration.cpp</c>.
/// Applies SC_CONCENTRATION; trap-reveal sub is TODO.
/// </summary>
public sealed class Concentration : SkillImpl
{
    public Concentration() : base(SkillIds.AC_CONCENTRATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Concentration, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
