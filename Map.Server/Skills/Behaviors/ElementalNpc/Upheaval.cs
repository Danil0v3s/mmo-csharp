using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_UPHEAVAL — Elemental Upheaval. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/upheaval.cpp</c>.
/// Toggles SC_UPHEAVAL on target + SC_UPHEAVAL_OPTION on the
/// elemental.
/// </summary>
public sealed class Upheaval : SkillImpl
{
    public Upheaval() : base(SkillIds.EL_UPHEAVAL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Upheaval) != null
            || ctx.Sc?.Get(src, StatusType.UpheavalOption) != null)
        {
            ctx.Sc.End(target, StatusType.Upheaval);
            ctx.Sc.End(src, StatusType.UpheavalOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.UpheavalOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Upheaval, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
