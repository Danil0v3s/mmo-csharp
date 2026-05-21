using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_PETROLOGY — Elemental Petrology. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/petrology.cpp</c>.
/// Toggles SC_PETROLOGY on target + SC_PETROLOGY_OPTION on the
/// elemental.
/// </summary>
public sealed class Petrology : SkillImpl
{
    public Petrology() : base(SkillIds.EL_PETROLOGY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Petrology) != null
            || ctx.Sc?.Get(src, StatusType.PetrologyOption) != null)
        {
            ctx.Sc.End(target, StatusType.Petrology);
            ctx.Sc.End(src, StatusType.PetrologyOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.PetrologyOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Petrology, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
