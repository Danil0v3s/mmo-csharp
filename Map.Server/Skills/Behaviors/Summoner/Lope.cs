using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_LOPE — Summoner Lope. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/lope.cpp</c>.
/// Teleports the caster to the target XY (gated on MF_NOTELEPORT;
/// CheckUnitMovePos handles cell reachability).
/// </summary>
public sealed class Lope : SkillImpl
{
    public Lope() : base(SkillIds.SU_LOPE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // NoTeleport mapflag refuses; otherwise place at the target cell
        // via UnitOps.CheckUnitMovePos which also walks to the nearest
        // walkable neighbour if (x,y) is blocked.
        if (ctx.World != null && ctx.MapFlags != null)
        {
            string? mapName = null;
            foreach (var m in ctx.World.All)
            {
                if ((uint)m.Name.GetHashCode() == src.MapId) { mapName = m.Name; break; }
            }
            if (mapName != null && ctx.MapFlags.IsSet(mapName, Map.Server.World.MapFlag.NoTeleport))
                return;
        }
        ctx.UnitOps?.CheckUnitMovePos(src, x, y, easy: 1);
    }
}
