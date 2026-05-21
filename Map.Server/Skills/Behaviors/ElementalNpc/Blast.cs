using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_BLAST — Elemental Blast. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/blast.cpp</c>.
/// Toggles SC_BLAST_OPTION on the elemental + SC_BLAST on the target;
/// if either is already up, ends both.
/// </summary>
public sealed class Blast : SkillImpl
{
    public Blast() : base(SkillIds.EL_BLAST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: SC_BLAST target SC not in StatusType yet; using BlastOption-only pair.
        if (ctx.Sc?.Get(src, StatusType.BlastOption) != null)
        {
            ctx.Sc.End(src, StatusType.BlastOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.BlastOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
