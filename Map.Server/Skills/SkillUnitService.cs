using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// First-slice ground-unit ticker. Each group's interval cadence is set
/// per skill (e.g. Storm Gust = 450 ms, Magnus = 1000 ms). Damage routes
/// through <see cref="IDamageService"/> so the AOI broadcast + death
/// pipeline matches normal combat.
///
/// The per-skill layout (square radius around the cast point) and per-
/// skill damage formula are hard-coded for the starter set; richer
/// shape data (line / cross / arc) plugs into <see cref="LayoutFor"/>
/// once those skills port.
/// </summary>
public sealed class SkillUnitService : ISkillUnitService
{
    private readonly IEntityRegistry _entities;
    private readonly IDamageService _damage;
    private readonly ILogger<SkillUnitService> _logger;
    private readonly List<SkillUnitGroup> _groups = new();

    public SkillUnitService(
        IEntityRegistry entities,
        IDamageService damage,
        ILogger<SkillUnitService> logger)
    {
        _entities = entities;
        _damage = damage;
        _logger = logger;
    }

    public SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short centerX, short centerY)
    {
        var spec = SpecFor(skillId);
        if (spec == null)
        {
            _logger.LogDebug("SkillUnitService.Place: no spec for skill {Skill}", skillId);
            return null;
        }

        var now = Environment.TickCount64;
        var group = new SkillUnitGroup
        {
            SkillId = skillId,
            SkillLevel = skillLevel,
            CasterId = caster.Id,
            MapId = caster.MapId,
            ExpiresAt = now + spec.DurationMs,
            IntervalMs = spec.IntervalMs,
        };

        var radius = spec.Radius;
        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                group.Units.Add(new SkillUnit
                {
                    Group = group,
                    X = (short)(centerX + dx),
                    Y = (short)(centerY + dy),
                    NextTick = now + spec.IntervalMs,
                });
            }
        }
        _groups.Add(group);
        return group;
    }

    public void Tick(long nowTick)
    {
        if (_groups.Count == 0) return;

        for (var i = _groups.Count - 1; i >= 0; i--)
        {
            var g = _groups[i];
            if (g.ExpiresAt <= nowTick)
            {
                _groups.RemoveAt(i);
                continue;
            }
            var spec = SpecFor(g.SkillId);
            if (spec == null) continue;

            var caster = _entities.Get(g.CasterId);

            foreach (var unit in g.Units)
            {
                if (unit.Removed) continue;
                if (unit.NextTick > nowTick) continue;
                unit.NextTick = nowTick + g.IntervalMs;

                // Find entities standing on this cell and apply the effect.
                // Cheap O(N) entity scan — fine for first slice; can swap
                // for AOI bucket lookup as the spatial index grows.
                foreach (var e in _entities.All())
                {
                    if (e.MapId != g.MapId) continue;
                    if (e.X != unit.X || e.Y != unit.Y) continue;
                    if (e.Id == g.CasterId) continue; // skip self
                    if (!IsValidVictim(e)) continue;

                    var dmg = spec.Damage(g.SkillLevel, caster, e);
                    if (dmg > 0) _damage.ApplyDamage(e, dmg, caster);
                }
            }
        }
    }

    // ---- per-skill specs ----

    private static SkillUnitSpec? SpecFor(ushort skillId) => skillId switch
    {
        // PR_MAGNUS — magic damage, holy element, ticks every 1s for 10s.
        SkillIds.PR_MAGNUSEXORCISMUS => new SkillUnitSpec(
            DurationMs: 10_000, IntervalMs: 1000, Radius: 2,
            Damage: (lvl, caster, _) => caster == null ? 0 : (int)(caster.Stats.MatkMin * (1 + lvl) / 5)),
        // WZ_STORMGUST — wind magic damage, ticks every 450ms for 4.5s.
        SkillIds.WZ_STORMGUST => new SkillUnitSpec(
            DurationMs: 4_500, IntervalMs: 450, Radius: 5,
            Damage: (lvl, caster, _) => caster == null ? 0 : (int)(caster.Stats.MatkMin * (lvl + 4) / 4)),
        _ => null,
    };

    private static bool IsValidVictim(Entity e) => e switch
    {
        MobEntity m => m.Hp > 0,
        PlayerEntity p => p.Hp > 0,
        _ => false,
    };

    private sealed record SkillUnitSpec(
        int DurationMs,
        int IntervalMs,
        int Radius,
        Func<ushort, Entity?, Entity, int> Damage);
}
