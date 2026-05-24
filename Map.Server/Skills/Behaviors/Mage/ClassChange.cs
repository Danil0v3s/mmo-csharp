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
        if (target is MobEntity mob && ctx.MobOps != null)
        {
            // rAthena: mob_class_change(target, mob_get_random_id(MOBG_CLASSCHANGE,...)).
            // The MOBG_CLASSCHANGE group YAML isn't loaded yet, so we fall through with
            // type=0 (unfiltered) — Deferred: proper mob-group filter table.
            var newClass = ctx.MobOps.GetRandomId(0, 0, 1);
            if (newClass > 0) ctx.MobOps.SetClass(mob, newClass);
        }
    }
}
