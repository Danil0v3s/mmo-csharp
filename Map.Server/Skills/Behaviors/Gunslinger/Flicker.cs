using Map.Server.Entities;
using Map.Server.Skills;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_FLICKER — Rebellion Flicker (skill.cpp:RL_FLICKER arm).
/// Detonates the caster's own Bind Traps (<c>RL_B_TRAP</c>) and
/// Howling Mines (<c>RL_H_MINE</c>) anywhere on the same map — they
/// don't need to be in a splash near the caster. Each marked group is
/// expired via <see cref="ISkillUnitService.DelUnitGroup"/>; the per-
/// trap onleft hook fires the actual damage.
/// </summary>
public sealed class Flicker : SkillImpl
{
    /// <summary>Whole-map sweep — rAthena flicker iterates every
    /// BL_SKILL on the map, not just nearby.</summary>
    private const short MapRadius = 1024;

    public Flicker() : base(SkillIds.RL_FLICKER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (ctx.Units == null) return;
        var groups = new HashSet<SkillUnitGroup>();
        foreach (var u in ctx.Units.GetUnitsInArea(pc.MapId, pc.X, pc.Y, MapRadius))
        {
            if (u.Group.CasterId != pc.Id) continue;
            if (u.Group.SkillId != SkillIds.RL_B_TRAP && u.Group.SkillId != SkillIds.RL_H_MINE) continue;
            groups.Add(u.Group);
        }
        foreach (var g in groups) ctx.Units.DelUnitGroup(g);
    }
}
