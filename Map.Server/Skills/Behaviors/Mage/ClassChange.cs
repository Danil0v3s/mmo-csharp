using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_CLASSCHANGE — Sage Class Change (Hocus Pocus). Morphs the
/// target mob into a random one from the MOBG_CLASSCHANGE group.
/// Status-immune mobs reject with skill_fail. Mob morph isn't
/// surfaced through SkillBehaviorContext yet — cast frame emits.
/// </summary>
public sealed class ClassChange : SkillImpl
{
    public ClassChange() : base(SkillIds.SA_CLASSCHANGE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity && (target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: mob_class_change(target, random_id_from(MOBG_CLASSCHANGE))
        // — requires mob class-mutation helper on IMobSpawnService.
    }
}
