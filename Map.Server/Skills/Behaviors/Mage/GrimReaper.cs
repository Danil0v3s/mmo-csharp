using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_DEATH — Sage Grim Reaper (Hocus Pocus). Instant-kills target unless status-immune.</summary>
public sealed class GrimReaper : SkillImpl
{
    public GrimReaper() : base(SkillIds.SA_DEATH) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity && (target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // status_kill: zero out target HP through the damage pipeline.
        if (target is PlayerEntity pc) ctx.Damage?.ApplyDamage(pc, pc.Hp, src);
        else if (target is MobEntity mob) ctx.Damage?.ApplyDamage(mob, mob.Hp, src);
    }
}
