using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-70 — SA_FREECAST cast-state attack-delay trigger. CalcPc precomputes a
/// freecast-adjusted attack delay (<see cref="BattleStats.FreecastAdelay"/>) via the
/// COMBAT-50 formula (the `5*(lv+10)%` ASPD-base scale), and AttackService swaps to it while
/// the PC is mid-cast.
/// </summary>
public class Combat70FreecastTests
{
    // ---- CalcPc precomputes FreecastAdelay ----

    [Fact]
    public void Low_level_freecast_is_slower_while_casting()
    {
        var pc = NewPc();
        pc.LearnedSkills[SkillIds.SA_FREECAST] = 1; // 55% ASPD while casting → slower
        new StatusCalcService().CalcPc(pc, Inputs());
        Assert.True(pc.Stats.FreecastAdelay > pc.Stats.Adelay);
    }

    [Fact]
    public void Freecast_level_10_is_full_speed()
    {
        var pc = NewPc();
        pc.LearnedSkills[SkillIds.SA_FREECAST] = 10; // 100% factor → no change
        new StatusCalcService().CalcPc(pc, Inputs());
        Assert.Equal(pc.Stats.Adelay, pc.Stats.FreecastAdelay);
    }

    [Fact]
    public void Without_freecast_the_mid_cast_delay_equals_the_normal_delay()
    {
        var pc = NewPc();
        new StatusCalcService().CalcPc(pc, Inputs());
        Assert.Equal(pc.Stats.Adelay, pc.Stats.FreecastAdelay);
    }

    // ---- AttackService selects the freecast delay only while mid-cast ----

    [Fact]
    public void AttackService_uses_freecast_delay_only_while_casting()
    {
        var cast = new StubCast();
        var svc = new AttackService(null!, null!, null!, NullLogger<AttackService>.Instance, cast: cast);
        var pc = NewPc();
        pc.Stats.Adelay = 1000;
        pc.Stats.FreecastAdelay = 1500;

        cast.Casting = true;
        Assert.Equal(1500, svc.NextSwingDelay(pc)); // mid-cast → freecast delay

        cast.Casting = false;
        Assert.Equal(1000, svc.NextSwingDelay(pc)); // not casting → normal delay
    }

    [Fact]
    public void AttackService_ignores_freecast_for_non_pc_or_unset_delay()
    {
        var cast = new StubCast { Casting = true };
        var svc = new AttackService(null!, null!, null!, NullLogger<AttackService>.Instance, cast: cast);

        // A PC with FreecastAdelay 0 (never computed) falls back to Adelay even while casting.
        var pc = NewPc();
        pc.Stats.Adelay = 1000;
        pc.Stats.FreecastAdelay = 0;
        Assert.Equal(1000, svc.NextSwingDelay(pc));

        // A mob (FreecastAdelay 0) always uses Adelay.
        var mob = new MobEntity(new EntityId(9), 1002, "Poring", 0, 0, 0);
        mob.Stats.Adelay = 800;
        Assert.Equal(800, svc.NextSwingDelay(mob));
    }

    // ---- helpers ----

    private static PlayerEntity NewPc() => new(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);

    private static PcBaseInputs Inputs() => new(
        BaseLevel: 99, JobLevel: 50,
        Str: 1, Agi: 90, Vit: 1, Int: 1, Dex: 50, Luk: 1,
        Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
        WeaponAtkMin: 0, WeaponAtkMax: 100, EquipDef: 0, EquipMdef: 0,
        AttackRange: 1, WeaponLevel: 0, WeaponType: 1);

    private sealed class StubCast : ISkillCastService
    {
        public bool Casting;
        public bool IsCasting(EntityId entityId) => Casting;
        public (ushort skillId, ushort skillLevel) GetCurrentCast(EntityId entityId) => (0, 0);
        public SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel) => SkillCastResult.Started;
        public bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel) => false;
        public void Tick(long nowTick) { }
        public bool CancelCast(EntityId entityId) => false;
    }
}
