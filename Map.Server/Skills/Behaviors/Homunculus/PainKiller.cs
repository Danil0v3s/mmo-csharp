using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_PAIN_KILLER — Homunculus Pain Killer. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_painkiller.cpp</c>.
/// Applies SC_PAIN_KILLER to the homunculus' master. Master lookup
/// pipeline is TODO; we apply to the named target.
/// </summary>
public sealed class PainKiller : SkillImpl
{
    public PainKiller() : base(SkillIds.MH_PAIN_KILLER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.PainKiller, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
