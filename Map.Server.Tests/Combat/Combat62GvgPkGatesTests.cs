using System;
using System.Collections.Generic;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-62 — the GvG/PK gates layered onto <see cref="ZoneDamageService"/>:
/// the <c>INF2_IGNOREGVGREDUCTION</c>/<c>INF2_IGNOREBGREDUCTION</c> zone-scaling
/// bypass (battle.cpp:2060 / 2150) and the PK damage rate
/// (<c>battle_calc_pk_damage</c>, battle.cpp:2158). The curated Inf2 overlay
/// (<see cref="SkillDb.LoadingFinished"/>) is verified end-to-end with the two
/// renewal skills that carry the flags (NJ_ZENYNAGE, GN_FIRE_EXPANSION_ACID).
/// </summary>
public class Combat62GvgPkGatesTests
{
    // ---- curated Inf2 overlay ----

    [Fact]
    public void Inf2_overlay_marks_zenynage_and_fire_expansion_acid()
    {
        var db = NewSkillDb();
        Assert.True(db.GetInf2(SkillIds.NJ_ZENYNAGE, SkillInf2.IgnoreGvgReduction));
        Assert.True(db.GetInf2(SkillIds.NJ_ZENYNAGE, SkillInf2.IgnoreBgReduction));
        Assert.True(db.GetInf2(SkillIds.GN_FIRE_EXPANSION_ACID, SkillInf2.IgnoreGvgReduction));
        Assert.True(db.GetInf2(SkillIds.GN_FIRE_EXPANSION_ACID, SkillInf2.IgnoreBgReduction));
        // A control skill carries neither flag.
        Assert.False(db.GetInf2(SkillIds.MG_FIREBOLT, SkillInf2.IgnoreGvgReduction));
    }

    // ---- INF2 ignore-reduction bypass ----

    [Fact]
    public void Gvg_ignore_reduction_skill_is_unscaled()
    {
        var zone = NewZone(MapFlag.Gvg);
        var pc = NewPlayer();

        // Control weapon skill → gvg_weapon 60%.
        Assert.Equal(60, zone.Scale(BattleAttackType.Weapon, pc, MakeMob(), 100,
            isSkill: true, isShortRange: true, skillId: SkillIds.MG_FIREBOLT));
        // INF2_IGNOREGVGREDUCTION skill → unscaled.
        Assert.Equal(100, zone.Scale(BattleAttackType.Weapon, pc, MakeMob(), 100,
            isSkill: true, isShortRange: true, skillId: SkillIds.NJ_ZENYNAGE));
    }

    [Fact]
    public void Bg_ignore_reduction_skill_is_unscaled()
    {
        var zone = NewZone(MapFlag.Battleground);
        var pc = NewPlayer();

        // Control → bg_weapon 60%.
        Assert.Equal(60, zone.Scale(BattleAttackType.Weapon, pc, MakeMob(), 100,
            isSkill: true, isShortRange: true, skillId: SkillIds.MG_FIREBOLT));
        // INF2_IGNOREBGREDUCTION skill → unscaled.
        Assert.Equal(100, zone.Scale(BattleAttackType.Weapon, pc, MakeMob(), 100,
            isSkill: true, isShortRange: true, skillId: SkillIds.GN_FIRE_EXPANSION_ACID));
    }

    // ---- PK damage rate ----

    [Fact]
    public void Pk_rate_reduces_pc_vs_pc_when_pk_mode_on()
    {
        var config = new BattleConfigService(NullLogger<BattleConfigService>.Instance);
        config.SetValue("pk_mode", 1);
        var zone = NewZone(zoneFlag: null, config); // non-zone map → isolate PK

        var src = NewPlayer();
        var target = NewPlayer();
        // pk_weapon_attack_damage_rate default 60.
        Assert.Equal(60, zone.Scale(BattleAttackType.Weapon, src, target, 100,
            isSkill: true, isShortRange: true, skillId: 0));
    }

    [Fact]
    public void Pk_rate_off_leaves_damage_unchanged()
    {
        var zone = NewZone(zoneFlag: null, new BattleConfigService(NullLogger<BattleConfigService>.Instance)); // pk_mode unset = 0
        var src = NewPlayer();
        var target = NewPlayer();
        Assert.Equal(100, zone.Scale(BattleAttackType.Weapon, src, target, 100,
            isSkill: true, isShortRange: true, skillId: 0));
    }

    [Fact]
    public void Pk_rate_ignored_when_target_is_not_a_player()
    {
        var config = new BattleConfigService(NullLogger<BattleConfigService>.Instance);
        config.SetValue("pk_mode", 1);
        var zone = NewZone(zoneFlag: null, config);

        var src = NewPlayer();
        // PC → mob is not PC↔PC, so PK does not apply.
        Assert.Equal(100, zone.Scale(BattleAttackType.Weapon, src, MakeMob(), 100,
            isSkill: true, isShortRange: true, skillId: 0));
    }

    [Fact]
    public void Pk_normal_attack_uses_short_and_long_rates()
    {
        var config = new BattleConfigService(NullLogger<BattleConfigService>.Instance);
        config.SetValue("pk_mode", 1);
        var zone = NewZone(zoneFlag: null, config);
        var src = NewPlayer();
        var target = NewPlayer();

        // Normal attack short → pk_short 80; long → pk_long 70.
        Assert.Equal(80, zone.Scale(BattleAttackType.Weapon, src, target, 100,
            isSkill: false, isShortRange: true, skillId: 0));
        Assert.Equal(70, zone.Scale(BattleAttackType.Weapon, src, target, 100,
            isSkill: false, isShortRange: false, skillId: 0));
    }

    [Fact]
    public void Pk_and_gvg_stack_for_pc_vs_pc_on_a_gvg_map()
    {
        var config = new BattleConfigService(NullLogger<BattleConfigService>.Instance);
        config.SetValue("pk_mode", 1);
        var zone = NewZone(MapFlag.Gvg, config);
        var src = NewPlayer();
        var target = NewPlayer();

        // gvg_weapon 60% then pk_weapon 60% → 100 → 60 → 36.
        Assert.Equal(36, zone.Scale(BattleAttackType.Weapon, src, target, 100,
            isSkill: true, isShortRange: true, skillId: SkillIds.MG_FIREBOLT));
    }

    // ---- helpers ----

    private const string ZoneMap = "woe";

    private static ZoneDamageService NewZone(MapFlag? zoneFlag, IBattleConfigService? config = null)
    {
        var map = new MapData(ZoneMap, 50, 50, new byte[50 * 50]);
        return new ZoneDamageService(new StubFlags(zoneFlag, ZoneMap), new StubWorld(map), config, NewSkillDb());
    }

    private static SkillDb NewSkillDb()
    {
        var db = new SkillDb();
        db.Register(Def(SkillIds.NJ_ZENYNAGE, "NJ_ZENYNAGE"));
        db.Register(Def(SkillIds.GN_FIRE_EXPANSION_ACID, "GN_FIRE_EXPANSION_ACID"));
        db.Register(Def(SkillIds.MG_FIREBOLT, "MG_FIREBOLT"), revalidate: true);
        return db;

        static SkillDefinition Def(ushort id, string name) => new()
        {
            Id = id, Name = name, MaxLevel = 10,
            Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Weapon,
        };
    }

    private static PlayerEntity NewPlayer()
    {
        var p = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        p.MapId = (uint)ZoneMap.GetHashCode();
        return p;
    }

    private static MobEntity MakeMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.MapId = (uint)ZoneMap.GetHashCode();
        return m;
    }

    private sealed class StubFlags : IMapFlagService
    {
        private readonly MapFlag? _flag;
        private readonly string _map;
        public StubFlags(MapFlag? flag, string map) { _flag = flag; _map = map; }
        public bool IsSet(string mapName, MapFlag flag)
            => _flag == flag && string.Equals(mapName, _map, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubWorld : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorld(params MapData[] maps) => _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
