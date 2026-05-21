using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_AQUAPLAY — Elemental Aqua Play. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/aquaplay.cpp</c>.
/// Toggles SC_AQUAPLAY on the target and SC_AQUAPLAY_OPTION on the
/// elemental itself.
/// </summary>
public sealed class AquaPlay : SkillImpl
{
    public AquaPlay() : base(SkillIds.EL_AQUAPLAY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Aquaplay) != null)
        {
            ctx.Sc.End(target, StatusType.Aquaplay);
            ctx.Sc.End(src, StatusType.AquaplayOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.AquaplayOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.Aquaplay, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
