using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_ODINS_RECALL — Odin's Recall (teleport-scroll variant,
/// skill.cpp:ALL_ODINS_RECALL arm).
/// Lv 1 → random warp on map (rAthena <c>pc_randomwarp</c>).
/// Lv 2 → savepoint warp (rAthena <c>pc_setpos(sd, sd->status.save_point.map)</c>).
/// Lv 3 → silent savepoint warp (same target, suppressed animation).
/// </summary>
public sealed class OdinsRecall : SkillImpl
{
    private readonly Random _rng;

    public OdinsRecall() : base(SkillIds.ALL_ODINS_RECALL) => _rng = Random.Shared;

    public OdinsRecall(Random? rng = null) : base(SkillIds.ALL_ODINS_RECALL) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        // Lv 3 = silent variant; suppress the skill-use packet.
        if (skillLevel < 3) ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (skillLevel == 1)
        {
            // Random warp on the current map — pick a walkable cell.
            if (ctx.World == null) return;
            foreach (var m in ctx.World.All)
            {
                if ((uint)m.Name.GetHashCode() != pc.MapId) continue;
                for (var tries = 0; tries < 32; tries++)
                {
                    var rx = (short)_rng.Next(0, m.Xs);
                    var ry = (short)_rng.Next(0, m.Ys);
                    if (!m.IsWalkable(rx, ry)) continue;
                    ctx.Setpos?.Setpos(pc, m.Name, rx, ry);
                    return;
                }
                return;
            }
            return;
        }
        // Lv 2 / 3 — savepoint warp.
        ctx.Death?.WarpToSavepoint(pc);
    }
}
