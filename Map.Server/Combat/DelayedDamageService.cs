using System.Collections.Concurrent;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IDelayedDamageService"/>. Queue + per-tick
/// drain. Each pending entry stores the resolved source/target ids
/// so a target that dies before the delay elapses gets skipped
/// (rAthena: <c>battle_delay_damage_sub</c> validates the bl at
/// fire time).
/// </summary>
public sealed class DelayedDamageService : IDelayedDamageService
{
    private readonly IDamageService _damage;
    private readonly IEntityRegistry _entities;
    private readonly ILogger<DelayedDamageService> _logger;
    private readonly ConcurrentQueue<Pending> _queue = new();

    public DelayedDamageService(
        IDamageService damage,
        IEntityRegistry entities,
        ILogger<DelayedDamageService> logger)
    {
        _damage = damage;
        _entities = entities;
        _logger = logger;
    }

    public void Schedule(Entity src, Entity target, long damage, int delayMs, ushort skillId)
    {
        if (damage <= 0) return;
        _queue.Enqueue(new Pending(
            SourceId: src.Id,
            TargetId: target.Id,
            Damage: damage,
            FireTick: Environment.TickCount64 + Math.Max(0, delayMs),
            SkillId: skillId));
    }

    public void DamageArea(Entity src, uint mapId, short centerX, short centerY, int radius, long damage, ushort skillId)
    {
        foreach (var e in _entities.All())
        {
            if (e.Id == src.Id) continue;
            if (e.MapId != mapId) continue;
            var dx = Math.Abs(e.X - centerX);
            var dy = Math.Abs(e.Y - centerY);
            if (Math.Max(dx, dy) > radius) continue;
            // rAthena delays the AoE bursts by 100ms so all
            // simultaneous visuals land together. Match it.
            Schedule(src, e, damage, 100, skillId);
        }
    }

    public void Tick(long nowTick)
    {
        // Drain only the entries whose deadline has elapsed; re-enqueue
        // the rest. ConcurrentQueue keeps insertion order; we use a
        // single pass over a snapshot.
        var snapshot = _queue.ToArray();
        if (snapshot.Length == 0) return;
        _queue.Clear();
        foreach (var p in snapshot)
        {
            if (p.FireTick > nowTick)
            {
                _queue.Enqueue(p);
                continue;
            }
            var src = _entities.Get(p.SourceId);
            var target = _entities.Get(p.TargetId);
            if (src == null || target == null) continue;
            _damage.ApplyDamage(target, (int)Math.Min(p.Damage, int.MaxValue), src);
        }
    }

    private readonly record struct Pending(EntityId SourceId, EntityId TargetId, long Damage, long FireTick, ushort SkillId);
}
