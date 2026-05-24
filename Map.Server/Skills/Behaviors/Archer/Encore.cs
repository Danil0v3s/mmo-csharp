using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_ENCORE — Bard/Dancer Encore (skill.cpp:11160). Re-triggers the
/// caster's last dance at no SP cost. Reads
/// <see cref="PlayerEntity.LastDanceSkillId"/> / Level (set when each
/// dance plugin runs). Refuses if no dance is on record.
/// </summary>
public sealed class Encore : SkillImpl
{
    public Encore() : base(SkillIds.BD_ENCORE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.LastDanceSkillId == 0) return;
        ctx.UnitOps?.SkillUseId(pc, target.Id, pc.LastDanceSkillId, pc.LastDanceSkillLevel);
    }
}
