using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.World;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_SHADOWJUMP — Shadow Leap. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowleap.cpp</c>.
///
/// <para>Teleport the caster to (x, y) iff the destination cell is
/// reachable and the map permits ad-hoc movement (rAthena: "You don't
/// move on GVG grounds." — refused on GvG / Battleground maps). On
/// success, end SC_HIDING.</para>
/// </summary>
public sealed class ShadowLeap : SkillImpl
{
    public ShadowLeap() : base(SkillIds.NJ_SHADOWJUMP) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: refuse on GVG / Battleground (gvg + bg_protected). C# port
        // currently exposes the Gvg flag; the dedicated Battleground enum row
        // lands when that flag ports. Until then the Gvg check covers WoE
        // castles (rAthena's broadest "no Shadow Leap" surface).
        if (IsTeleportRefusedHere(src, ctx))
        {
            if (src is PlayerEntity caster)
            {
                ctx.Client?.BroadcastSkillFail(caster, SkillId,
                    Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            }
            // Still end SC_HIDING — rAthena keeps the SC end side-effect.
            ctx.Sc?.End(src, StatusType.Hiding);
            return;
        }
        ctx.UnitOps?.CheckUnitMovePos(src, x, y, 1);
        ctx.Sc?.End(src, StatusType.Hiding);
    }

    private static bool IsTeleportRefusedHere(Entity src, SkillBehaviorContext ctx)
    {
        if (ctx.MapFlags == null || ctx.World == null) return false;
        foreach (var m in ctx.World.All)
        {
            if ((uint)m.Name.GetHashCode() == src.MapId)
            {
                return ctx.MapFlags.IsSet(m.Name, MapFlag.Gvg);
            }
        }
        return false;
    }
}
