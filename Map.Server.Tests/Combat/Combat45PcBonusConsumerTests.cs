using System;
using System.Collections.Generic;
using Core.Database.Entities;
using Core.Server.Packets;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Session;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors.Acolyte;
using Map.Server.Status;
using Map.Server.Tests.Session;
using Map.Server.Tests.Skills.Parity;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-45 — wiring the single-value pc_bonus consumers COMBAT-23 parsed:
/// bSpeedRate/bSpeedAddRate (move speed), bCriticalRate (crit %), bUseSPrate
/// (SP cost), bAddMaxWeight (weight cap), bHealPower2 (heal received).
/// (bUnbreakable* / bIntravision are COMBAT-65.)
/// </summary>
public class Combat45PcBonusConsumerTests
{
    // ---- speed ----

    [Fact]
    public void Speed_rate_makes_the_pc_faster()
    {
        var baseline = SpeedOf(speedRate: 0, speedAddRate: 0);
        Assert.Equal(150, baseline);
        // bSpeedRate is stored as the negative delta; -25 → speed_rate 75 → 112.
        Assert.Equal(112, SpeedOf(speedRate: -25, speedAddRate: 0));
        // bSpeedAddRate stacks on top.
        Assert.Equal(105, SpeedOf(speedRate: -25, speedAddRate: -5));
    }

    // ---- critical rate ----

    [Fact]
    public void Critical_rate_is_a_percent_modifier_on_crit()
    {
        var baseCri = CriOf(criticalRate: 0);
        Assert.True(baseCri > 0);
        Assert.Equal((short)(baseCri * 150 / 100), CriOf(criticalRate: 50)); // +50%
    }

    // ---- heal received ----

    [Fact]
    public void Heal_power2_boosts_heal_received_on_the_target()
    {
        var caster = NewPc(level: 50, intStat: 50);
        var target = NewPc(level: 50, intStat: 50);
        var heal = new Heal();

        Assert.Equal(600, heal.CalcRenewalHealForTest(caster, target, 10)); // base
        target.EquipBonuses.HealPower2 = 20;                                // target's bonus
        Assert.Equal(720, heal.CalcRenewalHealForTest(caster, target, 10)); // 600 × 1.20
    }

    // ---- SP cost ----

    [Fact]
    public void Use_sp_rate_reduces_the_skill_sp_cost()
    {
        const ushort skill = 9999;
        var db = new SkillDb();
        db.Register(new SkillDefinition
        {
            Id = skill, Name = "TEST_SP", MaxLevel = 1,
            Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Weapon,
            SpCost = new[] { 0, 100 },
        });
        var req = new SkillRequirementService(db, NullLogger<SkillRequirementService>.Instance);

        var caster = NewPc(level: 50, intStat: 50);
        caster.Stats.MaxSp = 200; caster.Sp = 50;

        Assert.False(req.CheckCondition(caster, skill, 1));  // needs 100, has 50
        caster.EquipBonuses.UseSpRate = -50;                 // -50% → cost 50
        Assert.True(req.CheckCondition(caster, skill, 1));   // needs 50, has 50
    }

    // ---- max weight ----

    [Fact]
    public void Add_max_weight_raises_the_weight_cap()
    {
        var (svc, pc) = WeightSetup(itemWeight: 15000);
        var plain = svc.UpdateWeightStatus(pc);          // 15000/20000 = 75% → overweight tier 1

        pc.EquipBonuses.AddMaxWeight = 10000;            // cap → 30000
        var raised = svc.UpdateWeightStatus(pc);          // 15000/30000 = 50% → not overweight (tier 0)

        Assert.Equal(1, plain);   // overweight (≥ 70%)
        Assert.Equal(0, raised);  // the higher cap removed the overweight tier
        Assert.True(raised < plain);
    }

    // ---- helpers ----

    private static int SpeedOf(int speedRate, int speedAddRate)
    {
        var calc = new StatusCalcService();
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.EquipBonuses.SpeedRate = speedRate;
        pc.EquipBonuses.SpeedAddRate = speedAddRate;
        calc.CalcPc(pc, Inputs(luk: 1));
        return pc.Stats.Speed;
    }

    private static short CriOf(int criticalRate)
    {
        var calc = new StatusCalcService();
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        pc.EquipBonuses.CriticalRate = criticalRate;
        calc.CalcPc(pc, Inputs(luk: 100));
        return pc.Stats.Cri;
    }

    private static PcBaseInputs Inputs(int luk) => new(
        BaseLevel: 99, JobLevel: 50,
        Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: luk,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
        AttackRange: 1);

    private static PlayerEntity NewPc(int level, int intStat)
    {
        var pc = new PlayerEntity(1, 1, "PC", Guid.NewGuid(), 0, 0, 0) { Level = level };
        pc.Stats.IntStat = (short)intStat;
        return pc;
    }

    private static (PlayerWeightStatusService svc, PlayerEntity pc) WeightSetup(int itemWeight)
    {
        var pc = new PlayerEntity(1, 1, "PC", Guid.NewGuid(), 0, 0, 0);
        var catalog = new StubCatalog();
        catalog.Set(500, new ItemEntity { Weight = (ushort)itemWeight });
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(sockets.ServerSide, 30000,
            new PacketSystem().Factory, new PacketSystem().Registry, NullLogger.Instance)
        {
            AccountId = 1, CharacterId = 1, EntityId = pc.Id, AuthState = MapAuthState.Spawned,
            Inventory = new List<Map.Server.Inventory.InventoryItem>
            {
                new() { NameId = 500, Amount = 1 },
            },
        };
        var sessions = new InMemorySessions();
        sessions.Register(pc.Id, session);
        var svc = new PlayerWeightStatusService(
            new RecordingStatusChangeService(new SkillTraceRecorder()),
            NullLogger<PlayerWeightStatusService>.Instance, catalog, sessions: sessions);
        return (svc, pc);
    }

    private sealed class StubCatalog : IItemCatalog
    {
        private readonly Dictionary<uint, ItemEntity> _byId = new();
        public void Set(uint id, ItemEntity row) => _byId[id] = row;
        public int Count => _byId.Count;
        public ItemEntity? Get(uint id) => _byId.GetValueOrDefault(id);
        public ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<ItemEntity> All() => _byId.Values;
        public void Reload() { }
    }

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _bySid = new();
        public void Register(EntityId id, MapSessionData s) => _bySid[id] = s;
        public MapSessionData? GetByEntityId(EntityId entityId) => _bySid.GetValueOrDefault(entityId);
    }
}
