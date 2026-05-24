using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Units;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Ground-unit ticker. Delegates per-skill behavior (lifetime, cadence,
/// radius, per-tick effect, step-on/step-off hooks) to
/// <see cref="ISkillUnitTickHandler"/> plugins registered via DI and
/// indexed by <see cref="SkillUnitTickRegistry"/>. The service owns
/// only the geometry + lifecycle scheduling; the per-skill logic lives
/// in <see cref="Skills.Units.Handlers"/>.
///
/// <para>Mirrors rAthena's <c>skill_unitsetting</c> → <c>skill_unit_onplace_timer</c>
/// → <c>skill_unit_onleft</c> → <c>skill_delunitgroup</c> lifecycle in
/// <c>src/map/skill.cpp</c>, with the dispatch table extracted into the
/// handler registry.</para>
/// </summary>
public sealed class SkillUnitService : ISkillUnitService
{
    private readonly IEntityRegistry _entities;
    private readonly SkillUnitTickRegistry _handlers;
    private readonly ISkillUnitContext _ctx;
    private readonly ISkillLayoutService? _layouts;
    private readonly ILogger<SkillUnitService> _logger;
    private readonly List<SkillUnitGroup> _groups = new();

    // Tracks (group, entityId) → last cell coordinates so we can fire
    // OnPlace exactly once per entry and OnLeft when the entity moves
    // off. Without this we'd retrigger SC_SAFETYWALL every tick the
    // entity stands on the cell.
    private readonly Dictionary<(SkillUnitGroup, EntityId), (short x, short y)> _presence = new();

    public SkillUnitService(
        IEntityRegistry entities,
        SkillUnitTickRegistry handlers,
        ISkillUnitContext ctx,
        ILogger<SkillUnitService> logger,
        ISkillLayoutService? layouts = null)
    {
        _entities = entities;
        _handlers = handlers;
        _ctx = ctx;
        _layouts = layouts;
        _logger = logger;
    }

    public SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short centerX, short centerY)
    {
        var h = _handlers.Get(skillId);
        if (h == null)
        {
            _logger.LogDebug("SkillUnitService.Place: no handler for skill {Skill}", skillId);
            return null;
        }

        var now = Environment.TickCount64;
        var group = new SkillUnitGroup
        {
            SkillId = skillId,
            SkillLevel = skillLevel,
            CasterId = caster.Id,
            MapId = caster.MapId,
            ExpiresAt = now + h.DurationMs(skillLevel),
            IntervalMs = h.IntervalMs(skillLevel),
        };

        var radius = (short)h.Radius(skillLevel);
        // SK.100-1b/d — consult ISkillLayoutService for per-skill
        // non-square shapes (FireWall row, IceWall cross, WallOfThorn
        // ring, FireBall plus, traps square). Falls back to the
        // legacy square radius when no layout service is wired or the
        // skill has no named layout.
        var offsets = _layouts?.GetLayoutForSkill(skillId, skillLevel, radius)
                      ?? BuildSquareFallback(radius);
        foreach (var (dx, dy) in offsets)
        {
            group.Units.Add(new SkillUnit
            {
                Group = group,
                X = (short)(centerX + dx),
                Y = (short)(centerY + dy),
                NextTick = now + h.IntervalMs(skillLevel),
            });
        }
        _groups.Add(group);
        return group;
    }

    /// <summary>Inline square-radius generator used when ISkillLayoutService is not wired.</summary>
    private static IReadOnlyList<(short Dx, short Dy)> BuildSquareFallback(short radius)
    {
        var cells = new List<(short, short)>((2 * radius + 1) * (2 * radius + 1));
        for (var dy = (short)-radius; dy <= radius; dy++)
            for (var dx = (short)-radius; dx <= radius; dx++)
                cells.Add((dx, dy));
        return cells;
    }

    public void Tick(long nowTick)
    {
        if (_groups.Count == 0) return;

        for (var i = _groups.Count - 1; i >= 0; i--)
        {
            var g = _groups[i];
            if (g.ExpiresAt <= nowTick)
            {
                EvictGroupPresence(g);
                _groups.RemoveAt(i);
                continue;
            }
            var h = _handlers.Get(g.SkillId);
            if (h == null) continue;

            var caster = _entities.Get(g.CasterId);

            foreach (var unit in g.Units)
            {
                if (unit.Removed) continue;
                if (unit.NextTick > nowTick) continue;
                unit.NextTick = nowTick + g.IntervalMs;

                // Cheap O(N) entity scan — fine for first slice; can swap
                // for AOI bucket lookup as the spatial index grows.
                foreach (var e in _entities.All())
                {
                    if (e.MapId != g.MapId) continue;
                    if (e.X != unit.X || e.Y != unit.Y) continue;
                    if (!h.IsValidVictim(caster, e)) continue;

                    var key = (g, e.Id);
                    if (!_presence.TryGetValue(key, out var prev) || prev.x != e.X || prev.y != e.Y)
                    {
                        // First time we've seen this entity on the cell
                        // (or it moved to a new cell within the same group).
                        h.OnPlace(caster, e, g.SkillLevel, nowTick, _ctx);
                        _presence[key] = (e.X, e.Y);
                    }
                    h.OnTick(caster, e, g.SkillLevel, nowTick, _ctx);
                }
            }

            // Detect step-offs: any (group, entity) entry whose entity is
            // no longer on a group cell triggers OnLeft + purge.
            ReapStepOffs(g, h, caster, nowTick);
        }
    }

    private void ReapStepOffs(SkillUnitGroup g, ISkillUnitTickHandler h, Entity? caster, long tick)
    {
        // Snapshot keys to allow mutation while iterating.
        var keys = _presence.Keys.Where(k => k.Item1 == g).ToList();
        foreach (var key in keys)
        {
            var entity = _entities.Get(key.Item2);
            var stillOn = entity != null
                && entity.MapId == g.MapId
                && g.Units.Any(u => !u.Removed && u.X == entity.X && u.Y == entity.Y);
            if (!stillOn)
            {
                if (entity != null)
                    h.OnLeft(caster, entity, g.SkillLevel, tick, _ctx);
                _presence.Remove(key);
            }
        }
    }

    private void EvictGroupPresence(SkillUnitGroup g)
    {
        var keys = _presence.Keys.Where(k => k.Item1 == g).ToList();
        foreach (var k in keys) _presence.Remove(k);
    }

    // -----------------------------------------------------------------
    // rAthena skill_unit_* movement / lifecycle helpers (skill.cpp).
    // -----------------------------------------------------------------

    public void UnitMove(Entity who, long tick, int flag)
    {
        // The tick loop already detects step-on / step-off through
        // _presence diffing, so this entry remains a no-op until an
        // event-driven step-on hook is wired in front of the polling.
    }

    public void UnitMoveUnit(SkillUnit unit, short newX, short newY)
    {
        unit.X = newX;
        unit.Y = newY;
    }

    public void UnitMoveUnitGroup(SkillUnitGroup group, short newX, short newY)
    {
        if (group.Units.Count == 0) return;
        var dx = (short)(newX - group.Units[0].X);
        var dy = (short)(newY - group.Units[0].Y);
        foreach (var u in group.Units)
        {
            u.X = (short)(u.X + dx);
            u.Y = (short)(u.Y + dy);
        }
    }

    public void UnitOnLeft(SkillUnit unit, Entity who, long tick)
    {
        var h = _handlers.Get(unit.Group.SkillId);
        if (h == null) return;
        var caster = _entities.Get(unit.Group.CasterId);
        h.OnLeft(caster, who, unit.Group.SkillLevel, tick, _ctx);
        _presence.Remove((unit.Group, who.Id));
    }

    public void UnitOnOut(SkillUnit unit, Entity who, long tick) => UnitOnLeft(unit, who, tick);

    public void UnitOnDamaged(SkillUnit unit, long damage)
    {
        var h = _handlers.Get(unit.Group.SkillId);
        if (h == null) return;
        h.OnDamaged(unit, damage, _ctx);
    }

    public void ClearUnitGroup(EntityId casterId)
    {
        for (var i = _groups.Count - 1; i >= 0; i--)
        {
            if (_groups[i].CasterId == casterId)
            {
                EvictGroupPresence(_groups[i]);
                _groups.RemoveAt(i);
            }
        }
    }

    public void DelUnit(SkillUnit unit) => unit.Removed = true;

    public void DelUnitGroup(SkillUnitGroup group)
    {
        foreach (var u in group.Units) u.Removed = true;
        EvictGroupPresence(group);
        _groups.Remove(group);
    }

    public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius)
    {
        var result = new List<SkillUnit>();
        foreach (var g in _groups)
        {
            if (g.MapId != mapId) continue;
            foreach (var u in g.Units)
            {
                if (u.Removed) continue;
                if (Math.Abs(u.X - cx) > radius) continue;
                if (Math.Abs(u.Y - cy) > radius) continue;
                result.Add(u);
            }
        }
        return result;
    }

    public IReadOnlyList<SkillUnit> GetUnitsInArea(uint mapId, short cx, short cy, short radius, ushort skillId)
    {
        var result = new List<SkillUnit>();
        foreach (var g in _groups)
        {
            if (g.MapId != mapId) continue;
            if (g.SkillId != skillId) continue;
            foreach (var u in g.Units)
            {
                if (u.Removed) continue;
                if (Math.Abs(u.X - cx) > radius) continue;
                if (Math.Abs(u.Y - cy) > radius) continue;
                result.Add(u);
            }
        }
        return result;
    }
}
