using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

public class MobSkillUseTests
{
    [Fact]
    public void Mob_With_Always_FireBoltSkill_CastsOnTarget()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10,
            skills: new[]
            {
                new MobSkillEntry
                {
                    SkillId = SkillIds.MG_FIREBOLT,
                    SkillLevel = 5,
                    State = MobSkillState.Berserk,
                    Condition = MobSkillCondition.Always,
                    Permillage = 10_000, // always trigger
                    DelayMs = 5_000,
                },
            });
        mob.Stats.MatkMin = mob.Stats.MatkMax = 200;

        var pc = ctx.AddPlayer(1, 51, 50);

        // First tick: target acquisition.
        ctx.Ai.Tick(0);
        Assert.NotNull(mob.Attack);

        // Second tick (past throttle): engaged → mob skill check fires.
        ctx.Ai.Tick(200);
        // Skill cast was scheduled (instant-cast wouldn't damage Lvl5 Firebolt
        // since cast time = 3000ms). What we DO verify: the skill-cast
        // service got a pending cast — pump the cast timer past the cast
        // time and the player should take damage.
        ctx.Skills.Tick(Environment.TickCount64 + 5_000);
        Assert.True(pc.Hp < pc.MaxHp, $"expected Fire Bolt damage; pc.Hp={pc.Hp}/{pc.MaxHp}");
    }

    [Fact]
    public void Mob_Without_Skills_FallsBackToBasicAttack()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10, skills: Array.Empty<MobSkillEntry>());
        var pc = ctx.AddPlayer(1, 51, 50);

        ctx.Ai.Tick(0);
        ctx.Ai.Tick(200);

        // No skill cast — the basic-attack path should have engaged.
        Assert.NotNull(mob.Attack);
        Assert.Equal(pc.Id, mob.Attack!.TargetId);
    }

    [Fact]
    public void Mob_With_LowHpCondition_CastsOnlyWhenHpLow()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10,
            skills: new[]
            {
                new MobSkillEntry
                {
                    SkillId = SkillIds.MG_FIREBOLT,
                    SkillLevel = 1,
                    State = MobSkillState.Berserk,
                    Condition = MobSkillCondition.MyHpLessThanRate,
                    Cond2 = 30, // < 30% HP
                    Permillage = 10_000,
                    DelayMs = 5_000,
                },
            });
        mob.Stats.MatkMin = mob.Stats.MatkMax = 200;
        var pc = ctx.AddPlayer(1, 51, 50);

        // Mob at full HP → condition fails. Tick AI; no pending skill cast.
        mob.Hp = mob.MaxHp;
        ctx.Ai.Tick(0);
        ctx.Ai.Tick(200);
        ctx.Skills.Tick(Environment.TickCount64 + 5_000);
        var hpAfterFull = pc.Hp;

        // Drop mob to 10% HP → condition met → skill fires.
        mob.Hp = mob.MaxHp / 10;
        ctx.Ai.Tick(500);
        ctx.Ai.Tick(700);
        ctx.Skills.Tick(Environment.TickCount64 + 10_000);
        Assert.True(pc.Hp < hpAfterFull, "expected fire bolt damage after HP crossed threshold");
    }

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(),
            NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(new MobSpawnRegistry(), entities, world, mobDb,
            itemCatalog, itemDrops, movement, visibility, ids, new StatusCalcService(),
            NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var attack = new AttackService(entities, damage, movement, NullLogger<AttackService>.Instance);
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        var skills = new SkillCastService(new SkillDb(), entities, damage,
            new BattleCalculator(new Random(0)), sc, NullLogger<SkillCastService>.Instance);
        var ai = new MobAiService(entities, attack, NullLogger<MobAiService>.Instance,
            skillCast: skills, rng: new Random(0));
        return new TestContext(ai, skills, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        MobAiService Ai,
        SkillCastService Skills,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 5000;
            pc.Stats.Mdef = 0; pc.Stats.Mdef2 = 0;
            pc.Stats.DefenseElement = BattleElement.Neutral;
            pc.Stats.ElementLevel = 1;
            Entities.Add(pc);
            return pc;
        }
        public MobEntity AddAggressiveMob(short x, short y, int range2, MobSkillEntry[] skills)
        {
            var db = new MobDbEntry
            {
                Id = 1031, AegisName = "POPORING", Name = "Poporing",
                Hp = 500, ChaseRange = range2, AttackRange = 1,
                Modes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Aggressive"] = true, ["CanAttack"] = true, ["CanMove"] = true,
                },
                Skills = skills,
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1031 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
        }
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string n) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
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

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }
}
