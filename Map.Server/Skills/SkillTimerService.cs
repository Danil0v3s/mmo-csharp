using System.Collections.Concurrent;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillTimerService"/>. Queue + per-tick drain
/// modelled on <see cref="Combat.DelayedDamageService"/>. Each pending
/// entry stores source / target ids (not entity references) so a
/// target that despawns before the deadline gets dropped.
///
/// <para>The callback is captured by-value at schedule time; per-skill
/// state (hit index, sub-mode, flag) rides on closure variables
/// rather than the rAthena <c>type</c>+<c>flag</c> int pair. This
/// keeps Sonic Blow's 8-hit ladder readable as a single for-loop
/// inside the skill body instead of a recursive switch jump.</para>
/// </summary>
public sealed class SkillTimerService : ISkillTimerService
{
    private readonly IEntityRegistry _entities;
    private readonly ILogger<SkillTimerService> _logger;
    private readonly ConcurrentQueue<Pending> _queue = new();

    public SkillTimerService(IEntityRegistry entities, ILogger<SkillTimerService> logger)
    {
        _entities = entities;
        _logger = logger;
    }

    public void Schedule(Entity src, Entity target, int delayMs, ushort skillId, ushort skillLevel,
        Action<Entity, Entity, ushort> callback)
    {
        _queue.Enqueue(new Pending(
            SourceId: src.Id,
            TargetId: target.Id,
            FireTick: Environment.TickCount64 + Math.Max(0, delayMs),
            SkillId: skillId,
            SkillLevel: skillLevel,
            Callback: callback));
    }

    public void Tick(long nowTick)
    {
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
            // rAthena guards inside skill_timerskill:
            //   if (src->prev == nullptr) break;  // off-map
            //   if (target->prev == nullptr) break;
            //   if (src->m != target->m) break;
            //   if (status_isdead(*src)) continue;  // most skills bail; some chain skills (CombatStep, TigerCannon) ignore
            //   if (status_isdead(*target) && skill_id != RG_INTIMIDATE && skill_id != WZ_WATERBALL) break;
            var src = _entities.Get(p.SourceId);
            var target = _entities.Get(p.TargetId);
            if (src == null || target == null) continue;
            if (src.MapId != target.MapId) continue;

            try
            {
                p.Callback(src, target, p.SkillLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skill timer callback for skillId {SkillId} threw", p.SkillId);
            }
        }
    }

    private readonly record struct Pending(
        EntityId SourceId,
        EntityId TargetId,
        long FireTick,
        ushort SkillId,
        ushort SkillLevel,
        Action<Entity, Entity, ushort> Callback);
}
