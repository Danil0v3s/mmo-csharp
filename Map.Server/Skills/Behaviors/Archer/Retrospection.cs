using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_RETROSPECTION — Trouvere Retrospection (Encore variant for the
/// chorus-song family). Re-triggers the caster's last chorus song via
/// <see cref="PlayerEntity.LastSongSkillId"/> / Level. Refuses if no
/// chorus is on record.
/// </summary>
public sealed class Retrospection : SkillImpl
{
    public Retrospection() : base(SkillIds.TR_RETROSPECTION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.LastSongSkillId == 0) return;
        ctx.UnitOps?.SkillUseId(pc, target.Id, pc.LastSongSkillId, pc.LastSongSkillLevel);
    }
}
