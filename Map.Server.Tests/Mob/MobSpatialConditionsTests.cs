using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Mob.Conditions;
using Map.Server.Spawn;

namespace Map.Server.Tests.Mob;

/// <summary>
/// T4.9b — unit tests for the MSC_MOBNEARBYGT and MSC_TRICKCASTING
/// evaluators. rAthena <c>mob.cpp:4377-4380</c>.
///
/// <para><c>MobNearbyGreater</c> uses <see cref="IEntityRegistry.ForEachInRange"/>
/// over the AREA_SIZE (≈14) Chebyshev box and returns true when the
/// in-range mob count (excluding self + dead) exceeds <c>cond2</c>.
/// <c>TrickCasting</c> reads <see cref="MobEntity.TrickCasting"/> &gt; 0
/// — the counter NPC_TRICKDEAD bumps.</para>
/// </summary>
public class MobSpatialConditionsTests
{
    [Fact]
    public void MobNearbyGreater_NoEntities_FalseWhenRegistryMissing()
    {
        // Defensive — same shape as MyStatusOn with no Sc supplied.
        var mob = MakeMob(0, 0);
        var ev = new MobNearbyGreaterCondition();
        var entry = MakeEntry(MobSkillCondition.MobNearbyGreater, cond2: 0);
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    [Fact]
    public void MobNearbyGreater_CountsOtherLiveMobs_ExcludesSelfAndDead()
    {
        var mob = MakeMob(0, 0, id: 1);
        var registry = new FakeEntityRegistry();
        registry.Add(mob);

        // 3 nearby live mobs.
        registry.Add(MakeMob(1, 1, id: 2));
        registry.Add(MakeMob(2, 2, id: 3));
        registry.Add(MakeMob(3, 3, id: 4));

        // 1 nearby DEAD mob — must not count.
        var corpse = MakeMob(4, 4, id: 5);
        corpse.Hp = 0;
        registry.Add(corpse);

        // 1 mob FAR outside AREA_SIZE — must not count.
        registry.Add(MakeMob(100, 100, id: 6));

        var ev = new MobNearbyGreaterCondition();
        var ctx = new MobConditionContext { Entities = registry };

        // cond2 = 2 → 3 live in range > 2 → true.
        Assert.True(ev.IsMet(mob, MakeEntry(MobSkillCondition.MobNearbyGreater, cond2: 2), ctx));
        // cond2 = 3 → 3 live in range NOT > 3 → false.
        Assert.False(ev.IsMet(mob, MakeEntry(MobSkillCondition.MobNearbyGreater, cond2: 3), ctx));
    }

    [Fact]
    public void TrickCasting_ReadsCounterDirectly()
    {
        var mob = MakeMob(0, 0);
        var ev = new TrickCastingCondition();
        var entry = MakeEntry(MobSkillCondition.TrickCasting);

        // Counter == 0 (default) → false.
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));

        // Counter > 0 → true. cond2 is ignored.
        mob.TrickCasting = 1;
        Assert.True(ev.IsMet(mob, entry, MobConditionContext.Empty));

        // Reset → false again.
        mob.TrickCasting = 0;
        Assert.False(ev.IsMet(mob, entry, MobConditionContext.Empty));
    }

    // --- helpers ---

    private static MobEntity MakeMob(short x, short y, int id = 1)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 100 };
        var origin = new MobSpawnEntry { MapId = 1, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(id), db, origin, mapId: 1, x: x, y: y);
        mob.MaxHp = 100;
        mob.Hp = 100;
        return mob;
    }

    private static MobSkillEntry MakeEntry(MobSkillCondition cond, int cond2 = 0)
        => new()
        {
            SkillId = 1,
            SkillLevel = 1,
            State = MobSkillState.Any,
            Condition = cond,
            Cond2 = cond2,
        };

    /// <summary>
    /// Tiny IEntityRegistry for the evaluator tests. The production
    /// EntityRegistry needs IMapWorldRegistry + MapData wiring just to
    /// build a spatial index; the evaluator only calls
    /// <see cref="IEntityRegistry.ForEachInRange"/> so a Chebyshev
    /// linear scan is plenty.
    /// </summary>
    private sealed class FakeEntityRegistry : IEntityRegistry
    {
        private readonly Dictionary<EntityId, Entity> _byId = new();

        public int Count => _byId.Count;
        public void Add(Entity e) => _byId[e.Id] = e;
        public Entity? Remove(EntityId id) { _byId.Remove(id, out var e); return e; }
        public Entity? Get(EntityId id) => _byId.GetValueOrDefault(id);
        public bool Contains(EntityId id) => _byId.ContainsKey(id);
        public IEnumerable<Entity> All() => _byId.Values;

        public void Move(EntityId id, short newX, short newY)
        {
            // No-op: the evaluator doesn't call Move; we only need
            // ForEachInRange to see the entities at their initial cells.
        }

        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask)
        {
            var result = new List<Entity>();
            foreach (var e in _byId.Values)
            {
                if (e.MapId != mapId) continue;
                if ((e.Type & mask) == 0) continue;
                if (Math.Abs(e.X - cx) > range || Math.Abs(e.Y - cy) > range) continue;
                result.Add(e);
            }
            return result;
        }

        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask)
            => Array.Empty<Entity>();
    }
}
