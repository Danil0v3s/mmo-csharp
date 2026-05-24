using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// HW_GANBANTEIN — High Wizard Ganbantein. Manual port of
/// <c>rathena-fork/src/map/skills/mage/ganbantein.cpp</c>.
///
/// <para>Removes every ground skill unit (Pneuma, Safety Wall, Fire Wall,
/// etc.) inside the splash at the target XY. Success rate is fixed at
/// 80 %; failure refunds the requirement. Removing the units requires
/// <c>skill_cell_overlap</c> which isn't exposed here — for now we
/// only land the success roll + animation broadcast.</para>
/// </summary>
public sealed class Ganbantein : SkillImpl
{
    private readonly Random _rng;

    public Ganbantein() : base(SkillIds.HW_GANBANTEIN) => _rng = Random.Shared;

    public Ganbantein(Random? rng = null) : base(SkillIds.HW_GANBANTEIN) => _rng = rng ?? Random.Shared;

    /// <summary>3×3 splash centered on the cast cell (rAthena
    /// HW_GANBANTEIN AREA_SIZE).</summary>
    private const short SplashRadius = 1;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) >= 80)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (ctx.Units == null) return;
        // rAthena skill_cell_overlap — walk every BL_SKILL unit in the
        // splash and DelUnitGroup it. The group reaper sweeps the
        // members at next tick.
        var units = ctx.Units.GetUnitsInArea(src.MapId, x, y, SplashRadius);
        var groups = new HashSet<SkillUnitGroup>();
        foreach (var u in units) groups.Add(u.Group);
        foreach (var g in groups) ctx.Units.DelUnitGroup(g);
    }
}
