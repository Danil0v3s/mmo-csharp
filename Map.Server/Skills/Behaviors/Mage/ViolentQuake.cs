using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_VIOLENT_QUAKE — Arch Mage Violent Quake. Manual port of
/// <c>rathena-fork/src/map/skills/mage/violentquake.cpp</c>.
///
/// <para>Drops an Earthquake ground unit + staggered rising-rock
/// sub-units (AG_VIOLENT_QUAKE_ATK). SC_CLIMAX modulation:
/// <list type="bullet">
///   <item>climax_lv = 1 — spawn a second rising rock per stagger.</item>
///   <item>climax_lv = 4 — inflict SC on enemies in range instead of damaging.</item>
///   <item>climax_lv = 5 — 7×7 spawn area (3-cell splash radius).</item>
/// </list>
/// </para>
/// </summary>
public sealed class ViolentQuake : SkillImpl
{
    public ViolentQuake() : base(SkillIds.AG_VIOLENT_QUAKE) { }

    /// <summary>rAthena <c>skill_get_splash</c>(AG_VIOLENT_QUAKE) — area
    /// from which rising rocks pick a random cell. Climax lv 5 expands
    /// it to 3.</summary>
    private const short DefaultArea = 2;

    /// <summary>rAthena <c>skill_get_time</c>(AG_VIOLENT_QUAKE) — total
    /// duration of the rising-rock cycle in ms.</summary>
    private const int UnitTimeMs = 5_000;

    /// <summary>rAthena <c>skill_get_unit_interval</c>(AG_VIOLENT_QUAKE)
    /// — stagger between rock spawns in ms.</summary>
    private const int UnitIntervalMs = 500;

    /// <summary>SC_CLIMAX_EARTH buff duration on the cast target
    /// (sc_start with time2 = 30s; skill_db Duration2 = 30000).</summary>
    private const int ClimaxEarthDurationMs = 30_000;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena violentquake.cpp:14 — sc_start(SC_CLIMAX_EARTH).
        ctx.Sc?.Start(target, StatusType.ClimaxEarth, val1: skillLevel, 0, 0, 0, durationMs: ClimaxEarthDurationMs, src);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Grab Climax's effect level if active (val1 carries the climax tier 1..5).
        var climaxLv = 0;
        var climaxSce = ctx.Sc?.Get(src, StatusType.Climax);
        if (climaxSce != null) climaxLv = climaxSce.Val1;

        // climax_lv == 5 expands the spawn area to 3 (7×7).
        var area = climaxLv == 5 ? (short)3 : DefaultArea;

        // Always drop the primary earthquake ground unit (visual + tick base).
        ctx.Units?.Place(src, SkillId, skillLevel, x, y);

        // climax_lv == 4 — no damage, inflict SC on enemies in splash.
        if (climaxLv == 4)
        {
            if (ctx.Sc != null)
            {
                var victims = ctx.Entities.ForEachInRange(src.MapId, x, y, area,
                    EntityType.Mob | EntityType.Pc);
                foreach (var v in victims)
                {
                    if (v.Id == src.Id) continue;
                    // SC_QUAKE_DEBUFF — the climax-4 status applied per rAthena
                    // skill_area_sub → skill_castend_nodamage_id dispatch. Falls
                    // back to SC_STUN when QuakeDebuff isn't yet enumerated.
                    var sc = LookupQuakeDebuff();
                    ctx.Sc.Start(v, sc, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
                }
            }
            return;
        }

        // Standard (and climax 1 doubles): spawn rising rocks at random
        // cells across the splash area, staggered by unit_interval.
        var iterations = UnitTimeMs / UnitIntervalMs;
        var rng = ctx.Battle is { } ? Random.Shared : Random.Shared;
        for (var i = 1; i <= iterations; i++)
        {
            SpawnRock(src, x, y, skillLevel, area, ctx, i, rng);
            if (climaxLv == 1)
            {
                // climax lv 1 — spawn a second rock alongside the first.
                SpawnRock(src, x, y, skillLevel, area, ctx, i, rng);
            }
        }
    }

    private static void SpawnRock(Entity src, short cx, short cy, ushort skillLevel,
        short area, SkillBehaviorContext ctx, int stagger, Random rng)
    {
        var tmpX = (short)(cx - area + rng.Next(area * 2 + 1));
        var tmpY = (short)(cy - area + rng.Next(area * 2 + 1));
        ctx.Units?.Place(src, SkillIds.AG_VIOLENT_QUAKE_ATK, skillLevel, tmpX, tmpY,
            delayMs: stagger * UnitIntervalMs);
    }

    /// <summary>SC_QUAKE_DEBUFF lookup — rAthena uses
    /// <c>SC_GROUND_QUAKE</c>'s sister state, but the precise enum name
    /// varies between sources. Falls back to Stun when the debuff isn't
    /// enumerated in our StatusType so the cast still applies a visible
    /// status — the dedicated SC drops in when the enum row lands.</summary>
    private static StatusType LookupQuakeDebuff()
    {
        // Project StatusType doesn't expose SC_QUAKE_DEBUFF today; the
        // Stun fallback is the closest visible debuff used by the
        // climax-4 disable branch.
        return StatusType.Stun;
    }
}
