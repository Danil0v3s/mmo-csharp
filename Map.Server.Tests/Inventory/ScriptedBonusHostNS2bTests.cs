using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Skills;
using Map.Server.Status;

namespace Map.Server.Tests.Inventory;

/// <summary>
/// NS-2b acceptance: the medium-volume host methods that were silent
/// no-ops pre-NS-2b — <c>skill</c> (726 NS-1b calls), <c>heal</c>,
/// <c>percentheal</c>, <c>itemheal</c>, <c>setoption</c>. These promote
/// the "host accepts the call but does nothing" pattern into real
/// behavior wires.
///
/// Cosmetic methods (<c>specialeffect</c>, <c>specialeffect2</c>,
/// <c>hateffect</c>, <c>message</c>, <c>dispbottom</c>, <c>petloot</c>)
/// stay as documented no-ops — each needs an AOI packet emitter and
/// will land in a separate wave when the surface is needed.
/// </summary>
public class ScriptedBonusHostNS2bTests
{
    private static PlayerEntity MakePc(int maxHp = 1000, int hp = 500, int maxSp = 400, int sp = 200)
    {
        var pc = new PlayerEntity(
            characterId: 1, accountId: 1, name: "Tester",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0);
        pc.MaxHp = maxHp;
        pc.Hp = hp;
        pc.MaxSp = maxSp;
        pc.Sp = sp;
        return pc;
    }

    // ===== heal =====

    [Fact]
    public void heal_adds_hp_and_sp_clamped_to_max()
    {
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.heal(300, 150);
        Assert.Equal(800, pc.Hp);
        Assert.Equal(350, pc.Sp);
    }

    [Fact]
    public void heal_clamps_to_MaxHp_when_overhealing()
    {
        var pc = MakePc(maxHp: 1000, hp: 900);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.heal(500, 0);
        Assert.Equal(1000, pc.Hp); // clamped, not 1400
    }

    [Fact]
    public void heal_clamps_to_zero_on_negative()
    {
        var pc = MakePc(hp: 100);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.heal(-500, 0);
        Assert.Equal(0, pc.Hp);
    }

    [Fact]
    public void heal_with_no_args_is_no_op()
    {
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.heal();
        Assert.Equal(500, pc.Hp);
    }

    // ===== percentheal =====

    [Fact]
    public void percentheal_applies_MaxHp_times_pct_over_100()
    {
        var pc = MakePc(maxHp: 1000, hp: 100, maxSp: 400, sp: 0);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.percentheal(50, 25);
        Assert.Equal(600, pc.Hp); // +500 (50% of 1000)
        Assert.Equal(100, pc.Sp); // +100 (25% of 400)
    }

    [Fact]
    public void percentheal_clamps_to_max()
    {
        var pc = MakePc(maxHp: 1000, hp: 900);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.percentheal(50, 0);
        Assert.Equal(1000, pc.Hp);
    }

    // ===== itemheal — should behave identically to heal in first slice =====

    [Fact]
    public void itemheal_is_same_as_heal_until_item_heal_rate_wires_in()
    {
        var pc = MakePc(maxHp: 1000, hp: 200);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.itemheal(300, 0);
        Assert.Equal(500, pc.Hp);
    }

    // ===== skill =====

    private sealed class StubSkillService : IPlayerSkillService
    {
        public ushort LastSkillId;
        public int LastLevel;
        public GrantKind LastKind;
        public int CallCount;
        public bool Grant(PlayerEntity pc, ushort skillId, int level, GrantKind kind = GrantKind.Permanent)
        {
            LastSkillId = skillId;
            LastLevel = level;
            LastKind = kind;
            CallCount++;
            return true;
        }
        public void Revoke(PlayerEntity pc, ushort skillId) { }
        public void CalcSkillTree(PlayerEntity pc) { }
        public void CleanSkillTree(PlayerEntity pc) { }
        public bool TryPlagiarize(PlayerEntity pc, ushort skillId, ushort skillLevel) => false;
        public void PlagiarismReset(PlayerEntity pc, byte type) { }
        public bool Validate(PlayerEntity pc, ushort skillId, int level) => true;
        public int CheckSkill(PlayerEntity? pc, ushort skillId) => pc?.LearnedSkills.GetValueOrDefault(skillId) ?? 0;
        public int CheckImperialGuard(PlayerEntity pc, ushort skillId) => 0;
        public int CheckSummoner(PlayerEntity pc, ushort skillType) => 0;
        public int GetEffectiveMaxLevel(string jobAegis, ushort skillId) => 10;
        public bool CheckSkillRequirements(string jobAegis, ushort skillId, PlayerEntity pc) => true;
    }

    [Fact]
    public void skill_resolves_aegis_name_via_SkillIds_reflection_and_grants_temp()
    {
        var pc = MakePc();
        var stub = new StubSkillService();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: null,
            skillSvc: stub);
        // SM_BASH = 5 per SkillIds.cs
        host.skill("SM_BASH", 7);
        Assert.Equal(1, stub.CallCount);
        Assert.Equal((ushort)5, stub.LastSkillId);
        Assert.Equal(7, stub.LastLevel);
        Assert.Equal(GrantKind.Temporary, stub.LastKind);
    }

    [Fact]
    public void skill_accepts_explicit_kind_arg()
    {
        var pc = MakePc();
        var stub = new StubSkillService();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: null,
            skillSvc: stub);
        host.skill("SM_BASH", 3, (int)GrantKind.Permanent);
        Assert.Equal(GrantKind.Permanent, stub.LastKind);
    }

    [Fact]
    public void skill_accepts_numeric_id_directly()
    {
        var pc = MakePc();
        var stub = new StubSkillService();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: null,
            skillSvc: stub);
        host.skill(5, 1);
        Assert.Equal((ushort)5, stub.LastSkillId);
    }

    [Fact]
    public void skill_with_unknown_name_does_not_throw_or_call_service()
    {
        var pc = MakePc();
        var stub = new StubSkillService();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: null,
            skillSvc: stub);
        host.skill("NOT_A_REAL_SKILL", 5);
        Assert.Equal(0, stub.CallCount);
    }

    [Fact]
    public void skill_without_service_is_silent_no_op()
    {
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        // No skillSvc — must not throw.
        host.skill("SM_BASH", 1);
    }

    // ===== setoption =====

    private sealed class StubOptionService : IPlayerOptionService
    {
        public PlayerOption? LastSet;
        public PlayerOption? LastAdd;
        public PlayerOption? LastRemove;
        public void SetOption(PlayerEntity pc, PlayerOption next) => LastSet = next;
        public void AddOption(PlayerEntity pc, PlayerOption bits) => LastAdd = bits;
        public void RemoveOption(PlayerEntity pc, PlayerOption bits) => LastRemove = bits;
        public void SetCart(PlayerEntity pc, int type) { }
        public void SetRiding(PlayerEntity pc, bool on) { }
        public void SetFalcon(PlayerEntity pc, bool on) { }
        public void SetWug(PlayerEntity pc, bool on) { }
        public void SetWugRider(PlayerEntity pc, bool on) { }
        public void SetMadogear(PlayerEntity pc, bool on) { }
        public void NotifyOption(PlayerEntity pc) { }
    }

    [Fact]
    public void setoption_with_single_arg_calls_SetOption()
    {
        var pc = MakePc();
        var stub = new StubOptionService();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: null,
            skillSvc: null, optionSvc: stub);
        host.setoption(0x100);
        Assert.Equal((PlayerOption)0x100, stub.LastSet);
    }

    [Fact]
    public void setoption_two_args_enable_calls_AddOption()
    {
        var pc = MakePc();
        var stub = new StubOptionService();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: null,
            skillSvc: null, optionSvc: stub);
        host.setoption(0x80, 1);
        Assert.Equal((PlayerOption)0x80, stub.LastAdd);
        Assert.Null(stub.LastRemove);
    }

    [Fact]
    public void setoption_two_args_disable_calls_RemoveOption()
    {
        var pc = MakePc();
        var stub = new StubOptionService();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle(),
            equipped: null, catalog: null, bonusSvc: null, entities: null,
            skillSvc: null, optionSvc: stub);
        host.setoption(0x80, 0);
        Assert.Equal((PlayerOption)0x80, stub.LastRemove);
        Assert.Null(stub.LastAdd);
    }

    [Fact]
    public void setoption_without_service_is_silent_no_op()
    {
        var pc = MakePc();
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.setoption(0x100);
    }

    // ===== cosmetic methods stay no-op + don't throw =====

    [Fact]
    public void cosmetic_methods_are_no_throw_no_ops()
    {
        var pc = MakePc(hp: 500, sp: 200);
        var host = new ScriptedBonusHost(pc, new EquipBonusBundle());
        host.specialeffect(1);
        host.specialeffect2(2);
        host.hateffect(3, 1);
        host.petloot(5);
        host.message("hi");
        host.dispbottom("hi");
        Assert.Equal(500, pc.Hp);
        Assert.Equal(200, pc.Sp);
    }
}
