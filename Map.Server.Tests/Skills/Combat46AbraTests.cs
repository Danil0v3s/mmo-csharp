using System;
using System.Collections.Generic;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors.Mage;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-46 — SA_ABRACADABRA (Hocus Pocus) rolls a skill from the abra_db pool
/// and dispatches it. The renewal abra_db is a flat uniform list (no per-level
/// weights), and SA_ABRACADABRA is never dispatched (re-entrancy guard).
/// </summary>
public class Combat46AbraTests
{
    [Fact]
    public void Casting_abracadabra_dispatches_the_rolled_skill()
    {
        var ex = new SkillExerciser(family: "Mage");
        var cast = new RecordingCast();
        var ctx = ex.Context with { Abra = new FixedAbra(SkillIds.SM_BASH), Cast = cast };

        new HocusPocus().CastendNoDamageId(ex.Caster, ex.Target, skillLevel: 5, ctx);

        Assert.Equal(SkillIds.SM_BASH, cast.Dispatched);
    }

    [Fact]
    public void Empty_pool_dispatches_nothing()
    {
        var ex = new SkillExerciser(family: "Mage");
        var cast = new RecordingCast();
        var ctx = ex.Context with { Abra = new FixedAbra(0, count: 0), Cast = cast };

        new HocusPocus().CastendNoDamageId(ex.Caster, ex.Target, skillLevel: 5, ctx);

        Assert.Null(cast.Dispatched);
    }

    [Fact]
    public void Abracadabra_is_never_dispatched_recursively()
    {
        // Defensive: even if the pool somehow contained Abracadabra, the guard
        // skips it (rAthena ud->skill_id != SA_ABRACADABRA).
        var ex = new SkillExerciser(family: "Mage");
        var cast = new RecordingCast();
        var ctx = ex.Context with { Abra = new FixedAbra(SkillIds.SA_ABRACADABRA), Cast = cast };

        new HocusPocus().CastendNoDamageId(ex.Caster, ex.Target, skillLevel: 5, ctx);

        Assert.Null(cast.Dispatched);
    }

    // ---- stubs ----

    private sealed class FixedAbra : IAbraDatabase
    {
        private readonly ushort _pick;
        public FixedAbra(ushort pick, int count = 1) { _pick = pick; Count = count; }
        public int Count { get; }
        public ushort? PickRandom(Random rng) => Count == 0 ? null : _pick;
        public void Reload() { }
    }

    private sealed class RecordingCast : ISkillCastService
    {
        public ushort? Dispatched { get; private set; }
        public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel)
        {
            Dispatched = skillId;
            return true; // claim success so ResolveSkillAt isn't also tried
        }
        public bool ResolveSkillAt(Entity source, short x, short y, ushort skillId, ushort skillLevel)
        {
            Dispatched = skillId;
            return true;
        }
        public void Tick(long nowTick) { }
        public bool CancelCast(EntityId entityId) => false;
        public bool IsCasting(EntityId entityId) => false;
        public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel)
            => default;
        public (ushort skillId, ushort skillLevel) GetCurrentCast(EntityId entityId) => default;
    }
}
