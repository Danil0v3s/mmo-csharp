using Map.Server.Entities;
using Map.Server.Skills.Splash;
using Map.Server.Tests.Visibility;
using Map.Server.World;

namespace Map.Server.Tests.Skills;

public class MapForeachInRangeServiceTests
{
    [Fact]
    public void ForEachEnemyInSplash_FindsMobInRange_SkipsCasterAndOutOfRange()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 100, 100);
        var nearMob = ctx.AddMob(102, 100, hp: 1000);  // 2 cells away
        var farMob = ctx.AddMob(110, 100, hp: 1000);   // 10 cells away — out of 3-range
        var deadMob = ctx.AddMob(101, 100, hp: 0);     // in range but already dead

        var hits = new List<Entity>();
        var count = ctx.Splash.ForEachEnemyInSplash(caster, 100, 100, range: 3, hits.Add);

        Assert.Equal(1, count);
        Assert.Contains(nearMob, hits);
        Assert.DoesNotContain(farMob, hits);
        Assert.DoesNotContain(deadMob, hits);
    }

    [Fact]
    public void ForEachAllyInSplash_FindsPartyMember_NotEnemy()
    {
        var ctx = Build();
        var caster = ctx.AddPlayer(1, 100, 100);
        caster.PartyId = 42;
        var partyMate = ctx.AddPlayer(2, 102, 100);
        partyMate.PartyId = 42;
        var enemy = ctx.AddPlayer(3, 103, 100);        // no party → enemy

        var hits = new List<Entity>();
        ctx.Splash.ForEachAllyInSplash(caster, 100, 100, range: 5, hits.Add);

        // Self counts as ally (BCT_SELF in BCT_NOENEMY).
        Assert.Contains(caster, hits);
        Assert.Contains(partyMate, hits);
        Assert.DoesNotContain(enemy, hits);
    }

    [Fact]
    public void MatchesMask_GuildMate_AsGuild()
    {
        var ctx = Build();
        var src = ctx.AddPlayer(1, 0, 0);
        src.GuildId = 7;
        var ally = ctx.AddPlayer(2, 0, 0);
        ally.GuildId = 7;

        Assert.True(ctx.Splash.MatchesMask(src, ally, BattleCheckTarget.Guild));
        Assert.False(ctx.Splash.MatchesMask(src, ally, BattleCheckTarget.Enemy));
    }

    [Fact]
    public void MatchesMask_NullSrc_TreatsTargetAsEnemy()
    {
        var ctx = Build();
        var target = ctx.AddPlayer(1, 0, 0);
        Assert.True(ctx.Splash.MatchesMask(null, target, BattleCheckTarget.Enemy));
    }

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var splash = new MapForeachInRangeService(entities);
        return new TestContext(splash, entities, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        IMapForeachInRangeService Splash,
        EntityRegistry Entities,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y, int hp)
        {
            var db = new Map.Server.Mob.MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = hp };
            var origin = new Map.Server.Spawn.MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(new EntityId((int)x * 1000 + (int)y), db, origin, MapId, x, y);
            mob.MaxHp = hp;
            mob.Hp = hp;
            Entities.Add(mob);
            return mob;
        }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
