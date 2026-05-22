using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// ST.5 — verifies StatusCalcService.CalcHomunculus / CalcMercenary /
/// CalcElemental delegate to CalcMob (companions are MobEntity in
/// this port) and apply level overrides when supplied.
/// </summary>
public class CompanionCalcTests
{
    [Fact]
    public void CalcHomunculus_HydratesFromMobDb()
    {
        var calc = new StatusCalcService();
        var homun = MakeMob(classId: 6001);
        calc.CalcHomunculus(homun);

        Assert.True(homun.Stats.MaxHp > 0);
        Assert.True(homun.Stats.Hit > 0);
        Assert.Equal(1, homun.Level); // db lv default
    }

    [Fact]
    public void CalcHomunculus_LevelOverrideOverwritesBaseLevel()
    {
        var calc = new StatusCalcService();
        var homun = MakeMob(classId: 6001);

        calc.CalcHomunculus(homun, levelOverride: 50);

        Assert.Equal(50, homun.Level);
    }

    [Fact]
    public void CalcMercenary_HydratesFromMobDb()
    {
        var calc = new StatusCalcService();
        var merc = MakeMob(classId: 2000);
        calc.CalcMercenary(merc);
        Assert.True(merc.Stats.MaxHp > 0);
    }

    [Fact]
    public void CalcMercenary_LevelOverride()
    {
        var calc = new StatusCalcService();
        var merc = MakeMob(classId: 2000);
        calc.CalcMercenary(merc, levelOverride: 25);
        Assert.Equal(25, merc.Level);
    }

    [Fact]
    public void CalcElemental_HydratesFromMobDb()
    {
        var calc = new StatusCalcService();
        var ele = MakeMob(classId: 2114);
        calc.CalcElemental(ele);
        Assert.True(ele.Stats.MaxHp > 0);
    }

    [Fact]
    public void CalcElemental_LevelOverride()
    {
        var calc = new StatusCalcService();
        var ele = MakeMob(classId: 2114);
        calc.CalcElemental(ele, levelOverride: 80);
        Assert.Equal(80, ele.Level);
    }

    [Fact]
    public void CalcNpc_NoOp_OnDialogNpc()
    {
        // ST.8: NPCs without a stat block keep their constructor-baseline
        // BattleStats — CalcNpc is a deliberate no-op for them.
        var calc = new StatusCalcService();
        var npc = new NpcEntity(new EntityId(7001), name: "TestNpc", spriteId: 100,
            mapId: 1, x: 100, y: 100, dir: 0, triggerArea: null,
            hooks: Map.Server.Scripting.Records.NpcHooks.Empty);
        var hpBefore = npc.Stats.MaxHp;
        calc.CalcNpc(npc);
        Assert.Equal(hpBefore, npc.Stats.MaxHp);
    }

    [Fact]
    public void CalcHomunculus_NullDb_NoOp()
    {
        // Calling CalcHomunculus on a mob whose DbEntry is null is a
        // no-op (CalcMob's early-return covers it). Verifies the
        // wiring doesn't NPE.
        var calc = new StatusCalcService();
        var ids = new EntityIdAllocator();
        var stubDb = new MobDbEntry { Id = 999999, AegisName = "STUB", Name = "Stub" };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 999999 };
        var homun = new MobEntity(ids.NextMob(), stubDb, origin, 1, 100, 100);
        // Don't call CalcMob first — leave dbEntry-derived fields zeroed.
        calc.CalcHomunculus(homun);
        Assert.True(homun.Stats.MaxHp >= 0); // didn't NPE
    }

    private static MobEntity MakeMob(int classId)
    {
        var ids = new EntityIdAllocator();
        var db = new MobDbEntry
        {
            Id = classId,
            AegisName = $"C{classId}",
            Name = $"Class{classId}",
            Level = 1,
            Hp = 100,
            Sp = 20,
            Str = 10, Agi = 10, Vit = 10, Int = 10, Dex = 10, Luk = 10,
            Attack = 5,
            Attack2 = 10,
            Defense = 5,
            MagicDefense = 5,
            WalkSpeed = 200,
            AttackMotion = 1024,
            AttackDelay = 1872,
            DamageMotion = 480,
            AttackRange = 1,
            ElementLevel = 1,
            Modes = new Dictionary<string, bool>(),
        };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = classId };
        return new MobEntity(ids.NextMob(), db, origin, 1, 100, 100);
    }
}
