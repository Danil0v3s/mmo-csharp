using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_ENCORE — Bard/Dancer Encore. Manual port of
/// <c>rathena-fork/src/map/skills/archer/encore.cpp</c>. Re-casts the
/// caster's last song. sd.skill_id_dance / skill_lv_dance bookkeeping
/// not wired here — broadcast only.
/// </summary>
public sealed class Encore : SkillImpl
{
    public Encore() : base(SkillIds.BD_ENCORE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: re-trigger sd.skill_id_dance at sd.skill_lv_dance — needs dance bookkeeping on PlayerEntity.
    }
}
