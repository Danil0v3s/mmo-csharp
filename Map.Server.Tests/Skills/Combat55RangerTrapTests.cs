using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Skills.Units;
using Map.Server.Skills.Units.Handlers;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-55 — Ranger trap damage (rAthena battle.cpp:9762). base = skill_lv*DEX +
/// INT*5, RE_LVL_TMDMOD above level 99, then the Research-Trap multiplier
/// (20*researchLv / 50|100 for a player; 0 with no Research Trap; 200/50|100 for a mob).
/// </summary>
public class Combat55RangerTrapTests
{
    [Fact]
    public void ClusterBomb_lv150_with_research_uses_tmdmod()
    {
        var pc = Ranger(dex: 100, intt: 50, level: 150, research: 5);
        // base = 5*100 + 50*5 = 750. TMDMOD(150) = 750*150/100 + 750*150/100 = 2250.
        // research: 2250 * 20*5 / 50 = 4500.
        Assert.Equal(4500, TrapDamage.Compute(SkillIds.RA_CLUSTERBOMB, 5, pc));
    }

    [Fact]
    public void FiringTrap_and_Icebound_use_divisor_100()
    {
        var pc = Ranger(dex: 100, intt: 50, level: 150, research: 5);
        // 2250 * 20*5 / 100 = 2250.
        Assert.Equal(2250, TrapDamage.Compute(SkillIds.RA_FIRINGTRAP, 5, pc));
        Assert.Equal(2250, TrapDamage.Compute(SkillIds.RA_ICEBOUNDTRAP, 5, pc));
    }

    [Fact]
    public void No_research_trap_means_zero_damage_for_a_player()
    {
        var pc = Ranger(dex: 100, intt: 50, level: 150, research: 0);
        Assert.Equal(0, TrapDamage.Compute(SkillIds.RA_CLUSTERBOMB, 5, pc));
    }

    [Fact]
    public void Below_level_100_skips_tmdmod()
    {
        var pc = Ranger(dex: 100, intt: 50, level: 99, research: 5);
        // base 750, no TMDMOD. ClusterBomb: 750 * 20*5 / 50 = 1500.
        Assert.Equal(1500, TrapDamage.Compute(SkillIds.RA_CLUSTERBOMB, 5, pc));
    }

    [Fact]
    public void Non_player_caster_uses_the_flat_200_multiplier()
    {
        var mob = new MobEntity(new EntityId(9), 1002, "Poring", 0, 0, 0);
        mob.Stats.Dex = 100; mob.Stats.IntStat = 50; mob.Level = 150;
        // base 750, TMDMOD 2250. ClusterBomb: 2250 * 200 / 50 = 9000.
        Assert.Equal(9000, TrapDamage.Compute(SkillIds.RA_CLUSTERBOMB, 5, mob));
        // FiringTrap: 2250 * 200 / 100 = 4500.
        Assert.Equal(4500, TrapDamage.Compute(SkillIds.RA_FIRINGTRAP, 5, mob));
    }

    [Fact]
    public void Unit_onplace_detonates_for_the_stepper()
    {
        var pc = Ranger(dex: 100, intt: 50, level: 150, research: 5);
        var victim = new MobEntity(new EntityId(5), 1002, "Poring", 0, 0, 0) { Hp = 100000, MaxHp = 100000 };
        var rec = new RecordingDamage();
        var unit = new ClusterBombUnit();
        var group = new SkillUnitGroup
        {
            SkillId = unit.SkillId, SkillLevel = 5, CasterId = pc.Id,
            MapId = 0, ExpiresAt = 0, IntervalMs = 1000,
        };

        unit.OnPlace(pc, victim, skillLevel: 5, tick: 0, new Ctx(rec), group);
        Assert.Equal(4500, rec.LastDamage);
        Assert.Same(victim, rec.LastTarget);
    }

    // ---- helpers ----

    private static PlayerEntity Ranger(short dex, short intt, int level, int research)
    {
        var pc = new PlayerEntity(1, 1, "Ranger", Guid.NewGuid(), 0, 0, 0);
        pc.Stats.Dex = dex; pc.Stats.IntStat = intt; pc.Level = level;
        if (research > 0) pc.LearnedSkills[SkillIds.RA_RESEARCHTRAP] = (byte)research;
        return pc;
    }

    private sealed class Ctx : ISkillUnitContext
    {
        public Ctx(IDamageService d) => Damage = d;
        public IDamageService Damage { get; }
        public Map.Server.Status.IStatusChangeService? Sc => null;
        public ISkillClientService? Client => null;
    }

    private sealed class RecordingDamage : IDamageService
    {
        public int LastDamage;
        public Entity? LastTarget;
        public int ApplyDamage(Entity target, int damage, Entity? source = null, int hits = 1)
        {
            LastDamage = damage; LastTarget = target; return damage;
        }
        public BattleDamage PerformMeleeAttack(Entity source, Entity target) => default;
    }
}
