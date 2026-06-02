using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// Wave 65 / Track A — equip-bonus aggregator: coma + autocast arrays.
/// Verifies that:
/// 1. <see cref="BonusScriptExtractor"/> populates
///    <see cref="EquipBonusBundle.ComaClass"/> /
///    <see cref="EquipBonusBundle.ComaRace"/> from `bonus2 bComa*`.
/// 2. <see cref="ScriptedBonusHost"/> populates
///    <see cref="EquipBonusBundle.AddEffOnAttack"/> /
///    <see cref="EquipBonusBundle.AddEffWhenHit"/> from
///    `bonus3 bAddEff*`.
/// 3. <see cref="BattleTargetService.CheckComa"/> rolls per-myriad
///    against ComaClass + ComaRace.
/// 4. <see cref="BattleEffectsService.AutocastAfterCast"/> invokes
///    <see cref="IPlayerBonusService.ExecuteAutobonus"/> for OnHit
///    AND iterates AddEffOnAttack to start SCs on the target.
/// </summary>
public class Wave65EquipBonusTrackATests
{
    private static PlayerEntity NewPc() =>
        new(characterId: 1, accountId: 1, name: "test",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0);

    private static MobEntity NewMob(BattleRace race = BattleRace.Demihuman,
        MobMode mode = MobMode.None)
    {
        var m = new MobEntity(new EntityId(2), classId: 1001, name: "test_mob",
            mapId: 0, x: 0, y: 0);
        m.Stats.Race = race;
        m.Stats.Mode = mode;
        m.Hp = m.MaxHp = 1000;
        return m;
    }

    // -- Bundle extractor coverage --

    [Fact]
    public void Bundle_bComaClass_PopulatesComaClassArray()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bComaClass,Class_Normal,200;", b);
        Assert.Equal(200, b.ComaClass[(int)BattleClassFlag.Normal]);
    }

    [Fact]
    public void Bundle_bComaRace_PopulatesComaRaceArray()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bComaRace,RC_DemiHuman,500;", b);
        Assert.Equal(500, b.ComaRace[(int)BattleRace.Demihuman]);
    }

    [Fact]
    public void Bundle_AddEffOnAttack_PopulatedByScriptedHost()
    {
        var pc = NewPc();
        var bundle = new EquipBonusBundle();
        var host = new ScriptedBonusHost(pc, bundle);
        // bonus3 bAddEff, Eff_Stun(=3), 500, 5000 — script engine
        // resolves Eff_Stun to its int id before the call lands.
        host.bonus3("bAddEff", (int)StatusType.Stun, 500, 5000);
        Assert.Single(bundle.AddEffOnAttack);
        Assert.Equal(StatusType.Stun, bundle.AddEffOnAttack[0].Sc);
        Assert.Equal((short)500, bundle.AddEffOnAttack[0].RatePermille);
        Assert.Equal(5000u, bundle.AddEffOnAttack[0].DurationMs);
    }

    [Fact]
    public void Bundle_AddEffWhenHit_PopulatedByScriptedHost()
    {
        var pc = NewPc();
        var bundle = new EquipBonusBundle();
        var host = new ScriptedBonusHost(pc, bundle);
        host.bonus3("bAddEffWhenHit", (int)StatusType.Poison, 100, 4000);
        Assert.Single(bundle.AddEffWhenHit);
        Assert.Equal(StatusType.Poison, bundle.AddEffWhenHit[0].Sc);
    }

    // -- CheckComa roll --

    [Fact]
    public void CheckComa_NoBundleBonuses_ReturnsFalse()
    {
        var svc = new BattleTargetService(
            entities: new StubEntityRegistry(),
            logger: NullLogger<BattleTargetService>.Instance,
            sc: null);
        var pc = NewPc();
        var mob = NewMob();
        Assert.False(svc.CheckComa(pc, mob));
    }

    [Fact]
    public void CheckComa_MobSourceAlwaysReturnsFalse()
    {
        // CheckComa requires src is PlayerEntity — coma is a PC-only
        // equip-bonus proc.
        var svc = new BattleTargetService(
            entities: new StubEntityRegistry(),
            logger: NullLogger<BattleTargetService>.Instance,
            sc: null);
        var srcMob = NewMob();
        var tgtMob = NewMob();
        Assert.False(svc.CheckComa(srcMob, tgtMob));
    }

    [Fact]
    public void CheckComa_FullRateAlwaysProcs()
    {
        // Set coma rate to 10 000 (= 100%) and assert the roll always
        // procs. The roll is `Rng.Next(10_000) >= rate` (false branch
        // returns) so when rate == 10_000 the only path is true.
        var svc = new BattleTargetService(
            entities: new StubEntityRegistry(),
            logger: NullLogger<BattleTargetService>.Instance,
            sc: null);
        var pc = NewPc();
        pc.EquipBonuses.ComaRace[(int)BattleRace.Demihuman] = 10_000;
        var mob = NewMob(BattleRace.Demihuman);
        Assert.True(svc.CheckComa(pc, mob));
    }

    [Fact]
    public void CheckComa_BossClassRoutesThroughComaClass()
    {
        var svc = new BattleTargetService(
            entities: new StubEntityRegistry(),
            logger: NullLogger<BattleTargetService>.Instance,
            sc: null);
        var pc = NewPc();
        pc.EquipBonuses.ComaClass[(int)BattleClassFlag.Boss] = 10_000;
        var mob = NewMob(mode: MobMode.Mvp);
        Assert.True(svc.CheckComa(pc, mob));
    }

    // -- AutocastAfterCast wiring --

    [Fact]
    public void AutocastAfterCast_InvokesExecuteAutobonusOnHit()
    {
        var bonusSvc = new RecordingBonusService();
        var damage = new NoopDamageService();
        var svc = new BattleEffectsService(
            damage: damage,
            logger: NullLogger<BattleEffectsService>.Instance,
            sc: null,
            bonusSvc: bonusSvc);
        var pc = NewPc();
        var mob = NewMob();
        svc.AutocastAfterCast(pc, mob);
        Assert.Single(bonusSvc.Calls);
        Assert.Equal(AutobonusTrigger.OnHit, bonusSvc.Calls[0]);
    }

    [Fact]
    public void AutocastAfterCast_StartsAddEffOnAttackEntries()
    {
        var bonusSvc = new RecordingBonusService();
        var damage = new NoopDamageService();
        var sc = new RecordingSc();
        var svc = new BattleEffectsService(
            damage: damage,
            logger: NullLogger<BattleEffectsService>.Instance,
            sc: sc,
            bonusSvc: bonusSvc);
        var pc = NewPc();
        // Rate 10 000 = guaranteed proc.
        pc.EquipBonuses.AddEffOnAttack.Add(new AddEffEntry(StatusType.Stun, 10_000, 5000));
        var mob = NewMob();
        svc.AutocastAfterCast(pc, mob);
        Assert.Single(sc.Started);
        Assert.Equal(StatusType.Stun, sc.Started[0].Type);
        Assert.Equal(5000, sc.Started[0].DurationMs);
        Assert.Same(mob, sc.Started[0].Target);
    }

    [Fact]
    public void AutocastElemBuff_InvokesExecuteAutobonusOnSkill()
    {
        var bonusSvc = new RecordingBonusService();
        var damage = new NoopDamageService();
        var svc = new BattleEffectsService(
            damage: damage,
            logger: NullLogger<BattleEffectsService>.Instance,
            sc: null,
            bonusSvc: bonusSvc);
        var pc = NewPc();
        svc.AutocastElemBuff(pc, skillId: 100);
        Assert.Single(bonusSvc.Calls);
        Assert.Equal(AutobonusTrigger.OnSkill, bonusSvc.Calls[0]);
    }

    // --- hand-rolled stubs ---

    private sealed class StubEntityRegistry : IEntityRegistry
    {
        public void Add(Entity entity) { }
        public Entity? Remove(EntityId id) => null;
        public Entity? Get(EntityId id) => null;
        public bool Contains(EntityId id) => false;
        public void Move(EntityId id, short newX, short newY) { }
        public IEnumerable<Entity> All() => Array.Empty<Entity>();
        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask) => Array.Empty<Entity>();
        public int Count => 0;
    }

    private sealed class NoopDamageService : IDamageService
    {
        public int ApplyDamage(Entity target, int damage, Entity? source = null, int hits = 1) => 0;
        public BattleDamage PerformMeleeAttack(Entity source, Entity target) => new();
    }

    private sealed class RecordingBonusService : IPlayerBonusService
    {
        public List<AutobonusTrigger> Calls { get; } = new();
        public bool AddBonusScript(PlayerEntity pc, string script, int durationMs, ushort iconType, bool persistent) => true;
        public void ClearBonusScripts(PlayerEntity pc, int flag) { }
        public bool AddAutobonus(PlayerEntity pc, AutobonusTrigger trigger, string script, int rate, int durationMs, ushort flag) => true;
        public void DelAutobonus(PlayerEntity pc, AutobonusTrigger trigger, bool restore) { }
        public void ExecuteAutobonus(PlayerEntity pc, AutobonusTrigger trigger) => Calls.Add(trigger);
    }

    private sealed class RecordingSc : IStatusChangeService
    {
        public List<StartCall> Started { get; } = new();
        public sealed record StartCall(Entity Target, StatusType Type, int DurationMs);

        public StatusChange? Start(Entity target, StatusType type, int val1, int val2, int val3, int val4, int durationMs, Entity? source = null, long nowTick = long.MinValue)
        {
            Started.Add(new StartCall(target, type, durationMs));
            return null;
        }
        public bool End(Entity target, StatusType type) => false;
        public StatusChange? Get(Entity target, StatusType type) => null;
        public void Tick(long nowTick) { }
        public int ClearAll(Entity target, byte type = 0) => 0;
        public int ClearBuffs(Entity target, SccbFlag flag) => 0;
        public int ClearOnChangeMap(Entity target) => 0;
        public int ClearOnLogout(Entity target) => 0;
        public int Spread(Entity source, Entity target) => 0;
        public int GetMaxStacks(StatusType type) => 1;
        public bool IsDisabledOnMap(uint mapId, StatusType type) => false;
        public int Refresh(Entity target) => 0;
        public ScfFlag GetEffectiveFlags(StatusType type) => ScfFlag.None;
    }
}
