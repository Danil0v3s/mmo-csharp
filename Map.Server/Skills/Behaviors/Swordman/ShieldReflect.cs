using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_REFLECTSHIELD — Crusader Reflect Shield. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldreflect.cpp</c>.
/// SC_DARKCROW blocks reflect skills — fail when the target is under
/// that SC. Otherwise defers to the StatusSkillImpl SC-apply path.
/// </summary>
public sealed class ShieldReflect : StatusSkillImpl
{
    public ShieldReflect() : base(SkillIds.CR_REFLECTSHIELD) { }

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
