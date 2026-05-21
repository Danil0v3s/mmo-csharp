using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_RETROSPECTION — Trouvere Retrospection (Encore variant for
/// Trouvere chorus songs). Manual port of
/// <c>rathena-fork/src/map/skills/archer/retrospection.cpp</c>.
/// Re-casts the last chorus song. sd.skill_id_song bookkeeping TODO.
/// </summary>
public sealed class Retrospection : SkillImpl
{
    public Retrospection() : base(SkillIds.TR_RETROSPECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: re-trigger sd.skill_id_song at sd.skill_lv_song.
    }
}
