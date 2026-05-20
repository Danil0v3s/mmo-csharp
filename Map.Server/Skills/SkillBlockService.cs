using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillBlockService"/>. Holds per-caster
/// (entity-id, skill-id) → resume-at maps and a list of deferred
/// timer-skill events. Mirrors rAthena <c>skill_blockpc_*</c> +
/// <c>skill_addtimerskill</c> (skill.cpp).
/// </summary>
public sealed class SkillBlockService : ISkillBlockService
{
    private readonly Dictionary<(EntityId, ushort), long> _pcBlock = new();
    private readonly Dictionary<(EntityId, ushort), long> _homunBlock = new();
    private readonly Dictionary<(EntityId, ushort), long> _mercBlock = new();
    private readonly List<DeferredCast> _timerEvents = new();
    private readonly ILogger<SkillBlockService> _logger;

    public SkillBlockService(ILogger<SkillBlockService> logger) => _logger = logger;

    private static void Start(Dictionary<(EntityId, ushort), long> map, EntityId id, ushort skill, int durationMs)
        => map[(id, skill)] = Environment.TickCount64 + Math.Max(0, durationMs);
    private static void Clear(Dictionary<(EntityId, ushort), long> map, EntityId id)
    {
        var keys = map.Keys.Where(k => k.Item1 == id).ToArray();
        foreach (var k in keys) map.Remove(k);
    }

    public void BlockPcStart(PlayerEntity pc, ushort skillId, int durationMs) => Start(_pcBlock, pc.Id, skillId, durationMs);
    public void BlockPcClear(PlayerEntity pc) => Clear(_pcBlock, pc.Id);
    public void BlockHomunStart(Entity homun, ushort skillId, int durationMs) => Start(_homunBlock, homun.Id, skillId, durationMs);
    public void BlockHomunClear(Entity homun) => Clear(_homunBlock, homun.Id);
    public void BlockMercStart(Entity merc, ushort skillId, int durationMs) => Start(_mercBlock, merc.Id, skillId, durationMs);
    public void BlockMercClear(Entity merc) => Clear(_mercBlock, merc.Id);

    public bool BlockCheck(Entity caster, ushort skillId)
    {
        var now = Environment.TickCount64;
        var key = (caster.Id, skillId);
        if (_pcBlock.TryGetValue(key, out var pc) && pc > now) return true;
        if (_homunBlock.TryGetValue(key, out var ho) && ho > now) return true;
        if (_mercBlock.TryGetValue(key, out var me) && me > now) return true;
        return false;
    }

    public bool DisableCheck(Entity caster, ushort skillId)
    {
        // rAthena's per-skill `disable_check` flag (skill_db.yml). The
        // bit isn't on SkillDefinition yet — when it ports this returns
        // (def.Flags & DISABLE) != 0. Default: not disabled.
        return false;
    }

    public void AddTimerSkill(Entity src, long fireAtTick, EntityId targetId, short x, short y, ushort skillId, ushort skillLevel, int flag)
    {
        _timerEvents.Add(new DeferredCast(src.Id, fireAtTick, targetId, x, y, skillId, skillLevel, flag));
    }

    public void ClearTimerSkill(Entity src)
    {
        _timerEvents.RemoveAll(e => e.SrcId == src.Id);
    }

    public void Tick(long nowTick)
    {
        if (_timerEvents.Count == 0) return;
        // Deferred events fire here; we just drop the entry and rely on
        // the caller-installed handler. A richer wiring lands when the
        // timer-skill resolver registry ports.
        _timerEvents.RemoveAll(e => e.FireAtTick <= nowTick);
    }

    private readonly record struct DeferredCast(
        EntityId SrcId,
        long FireAtTick,
        EntityId TargetId,
        short X,
        short Y,
        ushort SkillId,
        ushort SkillLevel,
        int Flag);
}
