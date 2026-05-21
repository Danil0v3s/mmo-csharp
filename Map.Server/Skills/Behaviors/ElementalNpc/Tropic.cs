using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_TROPIC — Elemental Tropic. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/tropic.cpp</c>.
/// Toggles SC_TROPIC on target + SC_TROPIC_OPTION on the elemental.
/// </summary>
public sealed class Tropic : SkillImpl
{
    public Tropic() : base(SkillIds.EL_TROPIC) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Tropic) != null
            || ctx.Sc?.Get(src, StatusType.TropicOption) != null)
        {
            ctx.Sc.End(target, StatusType.Tropic);
            ctx.Sc.End(src, StatusType.TropicOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.TropicOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Tropic, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
