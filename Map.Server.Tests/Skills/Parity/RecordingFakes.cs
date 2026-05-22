using System.Text.Json.Serialization;
using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Skills.Splash;
using Map.Server.Status;

namespace Map.Server.Tests.Skills.Parity;

/// <summary>
/// One observable event a <see cref="Map.Server.Skills.Behaviors.SkillImpl"/>
/// surfaced during a parity run. Order-sensitive — the harness diffs
/// the recorded sequence against the baseline JSON.
/// </summary>
public sealed record SkillTrace(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("data")] Dictionary<string, object?> Data);

/// <summary>
/// Bag of recording fakes — every call from a SkillImpl into the
/// shared services lands in <see cref="Events"/> as a <see cref="SkillTrace"/>.
/// The harness clears the bag before each exercise and snapshots the
/// post-run sequence.
/// </summary>
public sealed class SkillTraceRecorder
{
    public List<SkillTrace> Events { get; } = new();

    public void Record(string kind, Dictionary<string, object?> data)
        => Events.Add(new SkillTrace(kind, data));
}

// ----------------------------------------------------------------------
// Per-service recording fakes. Each one logs the inputs at call time
// and either returns a stub value or no-ops. Damage is the only one
// that needs a non-trivial return (skill chains check the dealt damage),
// so we route through a deterministic 1-to-1 formula.
// ----------------------------------------------------------------------

public sealed class RecordingDamageService : IDamageService
{
    private readonly SkillTraceRecorder _rec;
    public RecordingDamageService(SkillTraceRecorder rec) => _rec = rec;

    public int ApplyDamage(Entity target, int damage, Entity? source = null)
    {
        _rec.Record("damage", new()
        {
            ["target"] = (int)target.Id,
            ["source"] = (int?)source?.Id,
            ["damage"] = damage,
        });
        return damage;
    }

    public BattleDamage PerformMeleeAttack(Entity source, Entity target)
    {
        _rec.Record("melee", new()
        {
            ["source"] = (int)source.Id,
            ["target"] = (int)target.Id,
        });
        return new BattleDamage();
    }
}

public sealed class RecordingSkillAttackService : ISkillAttackService
{
    private readonly SkillTraceRecorder _rec;
    public RecordingSkillAttackService(SkillTraceRecorder rec) => _rec = rec;

    public long SkillAttack(BattleAttackType attackType, Entity source, Entity damageSource,
        Entity target, ushort skillId, ushort skillLevel, byte flag = 0)
    {
        _rec.Record("skill-attack", new()
        {
            ["attackType"] = attackType.ToString(),
            ["source"] = (int)source.Id,
            ["damageSource"] = (int)damageSource.Id,
            ["target"] = (int)target.Id,
            ["skillId"] = (int)skillId,
            ["skillLevel"] = (int)skillLevel,
        });
        return 0;
    }

    public int SkillAttackArea(Entity source, Entity centerTarget, ushort skillId, ushort skillLevel)
    {
        _rec.Record("skill-attack-area", new()
        {
            ["source"] = (int)source.Id,
            ["center"] = (int)centerTarget.Id,
            ["skillId"] = (int)skillId,
            ["skillLevel"] = (int)skillLevel,
        });
        return 0;
    }

    public int SkillAreaSub(Entity center, short range, Func<Entity, bool> onCell)
    {
        _rec.Record("skill-area-sub", new()
        {
            ["center"] = (int)center.Id,
            ["range"] = range,
        });
        return 0;
    }
}

public sealed class RecordingSkillClientService : ISkillClientService
{
    private readonly SkillTraceRecorder _rec;
    public RecordingSkillClientService(SkillTraceRecorder rec) => _rec = rec;

    public void BroadcastSkillNoDamage(Entity? src, Entity target, ushort skillId, int healOrLevel, bool success = true)
        => _rec.Record("nodamage", new()
        {
            ["src"] = (int?)src?.Id,
            ["target"] = (int)target.Id,
            ["skillId"] = (int)skillId,
            ["healOrLevel"] = healOrLevel,
            ["success"] = success,
        });

    public void BroadcastSkillDamage(Entity src, Entity target, ushort skillId, ushort skillLevel,
        long damage, int hitCount = 1, DamageActionType action = DamageActionType.SkillDamage)
        => _rec.Record("damage-broadcast", new()
        {
            ["src"] = (int)src.Id,
            ["target"] = (int)target.Id,
            ["skillId"] = (int)skillId,
            ["skillLevel"] = (int)skillLevel,
            ["damage"] = damage,
            ["hitCount"] = hitCount,
            ["action"] = action.ToString(),
        });

    public void BroadcastSkillFail(PlayerEntity caster, ushort skillId, SkillFailCause cause,
        int btype = 0, uint itemId = 0)
        => _rec.Record("fail", new()
        {
            ["caster"] = caster.CharacterId,
            ["skillId"] = (int)skillId,
            ["cause"] = cause.ToString(),
            ["btype"] = btype,
            ["itemId"] = (long)itemId,
        });

    public void BroadcastSkillCasting(Entity src, Entity? target, short targetX, short targetY,
        ushort skillId, ushort skillLevel, int castTimeMs)
        => _rec.Record("casting", new()
        {
            ["src"] = (int)src.Id,
            ["target"] = (int?)target?.Id,
            ["x"] = targetX,
            ["y"] = targetY,
            ["skillId"] = (int)skillId,
            ["skillLevel"] = (int)skillLevel,
            ["castTimeMs"] = castTimeMs,
        });

    public void BroadcastSkillCastCancel(Entity caster)
        => _rec.Record("cast-cancel", new()
        {
            ["caster"] = (int)caster.Id,
        });
}

public sealed class RecordingStatusChangeService : IStatusChangeService
{
    private readonly SkillTraceRecorder _rec;
    private readonly Dictionary<(int, StatusType), StatusChange> _active = new();

    public RecordingStatusChangeService(SkillTraceRecorder rec) => _rec = rec;

    public StatusChange? Start(Entity target, StatusType type, int val1, int val2,
        int val3, int val4, int durationMs, Entity? source = null,
        long nowTick = long.MinValue)
    {
        _rec.Record("sc-start", new()
        {
            ["target"] = (int)target.Id,
            ["type"] = type.ToString(),
            ["val1"] = val1, ["val2"] = val2, ["val3"] = val3, ["val4"] = val4,
            ["durationMs"] = durationMs,
            ["src"] = (int?)source?.Id,
        });
        var entry = new StatusChange { Type = type, Val1 = val1, Val2 = val2, Val3 = val3, Val4 = val4 };
        _active[((int)target.Id, type)] = entry;
        return entry;
    }

    public bool End(Entity target, StatusType type)
    {
        _rec.Record("sc-end", new() { ["target"] = (int)target.Id, ["type"] = type.ToString() });
        return _active.Remove(((int)target.Id, type));
    }

    public StatusChange? Get(Entity target, StatusType type)
        => _active.GetValueOrDefault(((int)target.Id, type));

    public void Tick(long nowTick) { /* unused */ }
}

public sealed class RecordingSplashService : IMapForeachInRangeService
{
    private readonly SkillTraceRecorder _rec;
    public RecordingSplashService(SkillTraceRecorder rec) => _rec = rec;

    public int ForEachInSplash(Entity? src, uint mapId, short centerX, short centerY,
        short range, BattleCheckTarget mask, EntityType entityMask, Action<Entity> action)
    {
        _rec.Record("splash", new()
        {
            ["src"] = (int?)src?.Id,
            ["centerX"] = centerX,
            ["centerY"] = centerY,
            ["range"] = range,
            ["mask"] = mask.ToString(),
        });
        return 0;
    }

    public bool MatchesMask(Entity? src, Entity target, BattleCheckTarget mask) => true;
}

public sealed class RecordingSkillUnitService : ISkillUnitService
{
    private readonly SkillTraceRecorder _rec;
    public RecordingSkillUnitService(SkillTraceRecorder rec) => _rec = rec;

    public Map.Server.Skills.SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short centerX, short centerY)
    {
        _rec.Record("unit-place", new()
        {
            ["caster"] = (int)caster.Id,
            ["skillId"] = (int)skillId,
            ["skillLevel"] = (int)skillLevel,
            ["centerX"] = centerX,
            ["centerY"] = centerY,
        });
        return null;
    }

    public void Tick(long nowTick) { }
    public void UnitMove(Entity who, long tick, int flag) { }
    public void UnitMoveUnit(Map.Server.Skills.SkillUnit unit, short newX, short newY) { }
    public void UnitMoveUnitGroup(Map.Server.Skills.SkillUnitGroup group, short newX, short newY) { }
    public void UnitOnLeft(Map.Server.Skills.SkillUnit unit, Entity who, long tick) { }
    public void UnitOnOut(Map.Server.Skills.SkillUnit unit, Entity who, long tick) { }
    public void UnitOnDamaged(Map.Server.Skills.SkillUnit unit, long damage) { }
    public void ClearUnitGroup(EntityId casterId) { }
    public void DelUnit(Map.Server.Skills.SkillUnit unit) { }
    public void DelUnitGroup(Map.Server.Skills.SkillUnitGroup group) { }
}

public sealed class RecordingUnitOpsService : Map.Server.Movement.UnitOps.IUnitOpsService
{
    private readonly SkillTraceRecorder _rec;
    public RecordingUnitOpsService(SkillTraceRecorder rec) => _rec = rec;

    public bool WalkToXy(Entity bl, short x, short y, byte flag)
    {
        _rec.Record("walk-xy", new() { ["bl"] = (int)bl.Id, ["x"] = x, ["y"] = y });
        return true;
    }
    public bool WalkToBl(Entity bl, Entity target, short range, byte flag)
    {
        _rec.Record("walk-bl", new() { ["bl"] = (int)bl.Id, ["target"] = (int)target.Id, ["range"] = range });
        return true;
    }
    public bool StopWalking(Entity bl, byte type)
    {
        _rec.Record("stop-walk", new() { ["bl"] = (int)bl.Id, ["type"] = type });
        return true;
    }
    public bool BlownBy(Entity bl, int direction, int count)
    {
        _rec.Record("blow", new() { ["bl"] = (int)bl.Id, ["dir"] = direction, ["count"] = count });
        return true;
    }
    public bool MovePos(Entity bl, short x, short y, byte easy, bool checkColl)
    {
        _rec.Record("move-pos", new() { ["bl"] = (int)bl.Id, ["x"] = x, ["y"] = y });
        return true;
    }

    // Methods on the interface that aren't called by the SkillImpl
    // bodies we exercise — no-op recorders.
    public int Warp(Entity bl, string mapName, short x, short y, byte flag) => 0;
    public bool StopAttack(Entity bl) => true;
    public bool CanMove(Entity bl) => true;
    public bool Attack(Entity src, EntityId targetId, byte continuous) => true;
    public bool SetDir(Entity bl, byte dir) => true;
    public byte GetDir(Entity bl) => 0;
    public bool SkillUseId(Entity src, EntityId targetId, ushort skillId, ushort skillLevel) => true;
    public bool SkillUsePos(Entity src, short x, short y, ushort skillId, ushort skillLevel) => true;
    public int RemoveMap(Entity bl, byte clearType) => 0;
    public int Free(Entity bl, byte clearType) => 0;
    public int ChangeViewSize(Entity bl, short size) => 0;
    public void DataCreate(Entity bl) { }
    public bool CanReachBl(Entity src, Entity target, short range) => true;
    public bool CanReachPos(Entity src, short x, short y, byte easy) => true;
}
