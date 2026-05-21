using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_GOLDENE_TONE — Homunculus Goldene Tone. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_goldenetone.cpp</c>.
/// Applies SC_GOLDENE_TONE to the homunculus' master. battle_get_master
/// is TODO; we apply to the named target.
/// </summary>
public sealed class GoldeneTone : SkillImpl
{
    public GoldeneTone() : base(SkillIds.MH_GOLDENE_TONE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.GoldeneTone, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
