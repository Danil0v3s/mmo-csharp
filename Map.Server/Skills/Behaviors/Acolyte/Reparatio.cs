using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_REPARATIO — Cardinal Reparatio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/reparatio.cpp</c>.
///
/// <para>Full-HP heal on a PC target. PC-only — mobs reject with
/// fail-broadcast. Status-immune targets heal for 0.</para>
/// </summary>
public sealed class Reparatio : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public Reparatio() : base(SkillIds.CD_REPARATIO) { }

    public Reparatio(IStatusOpsService? statusOps = null) : base(SkillIds.CD_REPARATIO)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        long healAmount = (target.Stats.Mode & MobMode.StatusImmune) != 0 ? 0 : target.Stats.MaxHp;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Client?.BroadcastSkillNoDamage(null, target, SkillIds.AL_HEAL, (int)healAmount);
        _statusOps?.Heal(target, healAmount, 0, 0);
    }
}
