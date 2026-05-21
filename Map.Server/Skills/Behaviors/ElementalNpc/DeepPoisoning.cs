using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_DEEP_POISONING — Elemental Deep Poisoning. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/deeppoisoning.cpp</c>.
/// Toggles SC_DEEP_POISONING on target + SC_DEEP_POISONING_OPTION on
/// the elemental.
/// </summary>
public sealed class DeepPoisoning : SkillImpl
{
    public DeepPoisoning() : base(SkillIds.EM_EL_DEEP_POISONING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.DeepPoisoning) != null
            || ctx.Sc?.Get(src, StatusType.DeepPoisoningOption) != null)
        {
            ctx.Sc.End(target, StatusType.DeepPoisoning);
            ctx.Sc.End(src, StatusType.DeepPoisoningOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.DeepPoisoningOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.DeepPoisoning, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
