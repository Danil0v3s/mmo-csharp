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
        // On a PvP map an unaffiliated player is a genuine BCT_ENEMY (excluded
        // from the BCT_NOENEMY ally splash); a party mate stays an ally.
        var ctx = Build(MapFlag.Pvp);
        var caster = ctx.AddPlayer(1, 100, 100);
        caster.PartyId = 42;
        var partyMate = ctx.AddPlayer(2, 102, 100);
        partyMate.PartyId = 42;
        var enemy = ctx.AddPlayer(3, 103, 100);        // no party, PvP map → enemy

        var hits = new List<Entity>();
        ctx.Splash.ForEachAllyInSplash(caster, 100, 100, range: 5, hits.Add);

        Assert.Contains(caster, hits);     // BCT_SELF
        Assert.Contains(partyMate, hits);  // BCT_PARTY
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

    // ---- SKILL-03: slave-mob ownership ----

    [Fact]
    public void Slave_FriendlyToMaster()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(1, 100, 100);
        var slave = ctx.AddMob(101, 100, hp: 1000);
        slave.MasterId = master.Id;

        // Master's offensive AoE must NOT hit its own slave.
        Assert.False(ctx.Splash.MatchesMask(master, slave, BattleCheckTarget.Enemy));
        Assert.True(ctx.Splash.MatchesMask(master, slave, BattleCheckTarget.Party));
        // ...and the slave's AoE must not hit the master.
        Assert.False(ctx.Splash.MatchesMask(slave, master, BattleCheckTarget.Enemy));
    }

    [Fact]
    public void Slave_AttacksWhatMasterWould_WildMob()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(1, 100, 100);
        var slave = ctx.AddMob(101, 100, hp: 1000);
        slave.MasterId = master.Id;
        var wild = ctx.AddMob(102, 100, hp: 1000);

        // The slave is hostile to a wild mob (as the player would be).
        Assert.True(ctx.Splash.MatchesMask(slave, wild, BattleCheckTarget.Enemy));
    }

    [Fact]
    public void SameMasterSlavesAreParty()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(1, 100, 100);
        var a = ctx.AddMob(101, 100, hp: 1000); a.MasterId = master.Id;
        var b = ctx.AddMob(102, 100, hp: 1000); b.MasterId = master.Id;

        Assert.True(ctx.Splash.MatchesMask(a, b, BattleCheckTarget.Party));
        Assert.False(ctx.Splash.MatchesMask(a, b, BattleCheckTarget.Enemy));
    }

    [Fact]
    public void Slave_FriendlyToMastersParty()
    {
        var ctx = Build();
        var master = ctx.AddPlayer(1, 100, 100); master.PartyId = 9;
        var mate = ctx.AddPlayer(2, 101, 100); mate.PartyId = 9;
        var slave = ctx.AddMob(102, 100, hp: 1000); slave.MasterId = master.Id;

        Assert.True(ctx.Splash.MatchesMask(slave, mate, BattleCheckTarget.Party));
        Assert.False(ctx.Splash.MatchesMask(slave, mate, BattleCheckTarget.Enemy));
    }

    // ---- SKILL-03: PvP / field / friendly-fire mapflags ----

    [Fact]
    public void FieldMap_SuppressesPlayerSplash()
    {
        var ctx = Build(); // no pvp/gvg flag
        var a = ctx.AddPlayer(1, 100, 100);
        var b = ctx.AddPlayer(2, 101, 100); // unaffiliated
        // On a peaceful field map, an offensive AoE does NOT hit a stranger.
        Assert.False(ctx.Splash.MatchesMask(a, b, BattleCheckTarget.Enemy));
        Assert.True(ctx.Splash.MatchesMask(a, b, BattleCheckTarget.Neutral));
    }

    [Fact]
    public void PvpMap_EnablesPlayerSplash()
    {
        var ctx = Build(MapFlag.Pvp);
        var a = ctx.AddPlayer(1, 100, 100);
        var b = ctx.AddPlayer(2, 101, 100);
        Assert.True(ctx.Splash.MatchesMask(a, b, BattleCheckTarget.Enemy));
    }

    [Fact]
    public void PvpMap_PartyMate_NotEnemy_UnlessNoparty()
    {
        var noff = Build(MapFlag.Pvp);
        var a1 = noff.AddPlayer(1, 100, 100); a1.PartyId = 5;
        var b1 = noff.AddPlayer(2, 101, 100); b1.PartyId = 5;
        Assert.False(noff.Splash.MatchesMask(a1, b1, BattleCheckTarget.Enemy)); // protected

        var ff = Build(MapFlag.Pvp, MapFlag.PvpNoparty);
        var a2 = ff.AddPlayer(1, 100, 100); a2.PartyId = 5;
        var b2 = ff.AddPlayer(2, 101, 100); b2.PartyId = 5;
        Assert.True(ff.Splash.MatchesMask(a2, b2, BattleCheckTarget.Enemy)); // friendly-fire on
    }

    [Fact]
    public void GvgNoparty_FriendlyFire_MakesPartyHittable()
    {
        var protectedCtx = Build(MapFlag.Gvg);
        var a1 = protectedCtx.AddPlayer(1, 100, 100); a1.PartyId = 3;
        var b1 = protectedCtx.AddPlayer(2, 101, 100); b1.PartyId = 3;
        Assert.False(protectedCtx.Splash.MatchesMask(a1, b1, BattleCheckTarget.Enemy));

        var woe = Build(MapFlag.Gvg, MapFlag.GvgNoparty);
        var a2 = woe.AddPlayer(1, 100, 100); a2.PartyId = 3;
        var b2 = woe.AddPlayer(2, 101, 100); b2.PartyId = 3;
        Assert.True(woe.Splash.MatchesMask(a2, b2, BattleCheckTarget.Enemy));
    }

    [Fact]
    public void GvgMap_GuildMate_AlwaysAlly()
    {
        var ctx = Build(MapFlag.Gvg, MapFlag.GvgNoparty);
        var a = ctx.AddPlayer(1, 100, 100); a.GuildId = 11;
        var b = ctx.AddPlayer(2, 101, 100); b.GuildId = 11;
        // Guildmates are WoE allies even with gvg_noparty.
        Assert.True(ctx.Splash.MatchesMask(a, b, BattleCheckTarget.Guild));
        Assert.False(ctx.Splash.MatchesMask(a, b, BattleCheckTarget.Enemy));
    }

    private static TestContext Build(params MapFlag[] flags)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var mapFlags = new StubMapFlags(mapName, flags);
        var splash = new MapForeachInRangeService(entities, mapFlags, world);
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

    private sealed class StubMapFlags : IMapFlagService
    {
        private readonly string _map;
        private readonly HashSet<MapFlag> _flags;
        public StubMapFlags(string map, IEnumerable<MapFlag> flags)
        {
            _map = map;
            _flags = new HashSet<MapFlag>(flags);
        }
        public bool IsSet(string mapName, MapFlag flag)
            => string.Equals(mapName, _map, StringComparison.OrdinalIgnoreCase) && _flags.Contains(flag);
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
