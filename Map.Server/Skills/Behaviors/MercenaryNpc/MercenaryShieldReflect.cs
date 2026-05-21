using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MS_REFLECTSHIELD — Mercenary Reflect Shield. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_shieldreflect.cpp</c>.
/// Refuses to land on targets affected by SC_DARKCROW; otherwise
/// delegates to the StatusSkillImpl SC-apply path.
/// </summary>
public sealed class MercenaryShieldReflect : StatusSkillImpl
{
    public MercenaryShieldReflect() : base(SkillIds.MS_REFLECTSHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Darkcrow) != null)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        base.CastendNoDamageId(src, target, skillLevel, ctx);
    }
}
