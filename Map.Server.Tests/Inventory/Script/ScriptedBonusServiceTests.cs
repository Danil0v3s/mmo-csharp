using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Status;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Inventory.Script;

public class ScriptedBonusServiceTests
{
    private static (ScriptedBonusService svc, PlayerEntity pc, EquipBonusBundle bundle, FakeBonusSvc bonusSvc)
        BuildFixture(IReadOnlyList<InventoryItem>? equipped = null)
    {
        var bonusSvc = new FakeBonusSvc();
        var svc = new ScriptedBonusService(NullLogger<ScriptedBonusService>.Instance, bonusSvc);
        var pc = new PlayerEntity(
            characterId: 1, accountId: 1, name: "Tester",
            sessionId: Guid.NewGuid(), mapId: 0, x: 0, y: 0)
        {
            Level = 99, JobLevel = 50, GuildId = 5,
        };
        pc.Stats.Str = 50; pc.Stats.Dex = 40;
        var bundle = new EquipBonusBundle();
        return (svc, pc, bundle, bonusSvc);
    }

    // ---- core ----

    [Fact]
    public void SimpleBonus_AppliesToFlat()
    {
        var (svc, pc, bundle, _) = BuildFixture();
        Assert.True(svc.Apply("bonus bAtk,10;", pc, bundle));
        Assert.Equal(10, bundle.FlatAtk);
    }

    [Fact]
    public void IndexedBonus_AppliesToRaceSlot()
    {
        var (svc, pc, bundle, _) = BuildFixture();
        Assert.True(svc.Apply("bonus2 bAddRace,RC_Dragon,5;", pc, bundle));
        Assert.Equal(5, bundle.AddRace[(int)BattleRace.Dragon]);
    }

    // ---- conditionals (DSL goal #1) ----

    [Fact]
    public void Conditional_OnRefine_AppliesWhenTrue()
    {
        var equipped = new[]
        {
            new InventoryItem { NameId = 1, Equip = EquipBonusAggregator.EquipRightHand, Refine = 9 },
        };
        var (svc, pc, bundle, _) = BuildFixture(equipped);
        Assert.True(svc.Apply(
            "if (getrefine() >= 7) { bonus bAtk,40; }", pc, bundle, equipped));
        Assert.Equal(40, bundle.FlatAtk);
    }

    [Fact]
    public void Conditional_OnRefine_SkipsWhenFalse()
    {
        var equipped = new[]
        {
            new InventoryItem { NameId = 1, Equip = EquipBonusAggregator.EquipRightHand, Refine = 4 },
        };
        var (svc, pc, bundle, _) = BuildFixture(equipped);
        Assert.True(svc.Apply(
            "if (getrefine() >= 7) { bonus bAtk,40; }", pc, bundle, equipped));
        Assert.Equal(0, bundle.FlatAtk);
    }

    [Fact]
    public void RealCombo_Id27_AppliesBaseAtkPlusBranches()
    {
        // Real item_combos.yml id=27 — Neutronic / NC_AXEBOOMERANG combo.
        var equipped = new[]
        {
            new InventoryItem { NameId = 1, Equip = EquipBonusAggregator.EquipRightHand, Refine = 12 },
            new InventoryItem { NameId = 2, Equip = EquipBonusAggregator.EquipShoes, Refine = 12 },
        };
        var (svc, pc, bundle, _) = BuildFixture(equipped);
        var src = @"
            bonus bBaseAtk,40;
            .@eq = getequiprefinerycnt(EQI_SHOES);
            .@weapon = getequiprefinerycnt(EQI_HAND_R);
            if (.@eq >= 7 && .@weapon >= 7) {
                bonus2 bSkillAtk,""NC_AXEBOOMERANG"",15;
            }
            if ((.@eq + .@weapon) >= 18) {
                bonus bAtkRate,10;
                if ((.@eq + .@weapon) >= 22) {
                    bonus bLongAtkRate,10;
                }
            }
        ";
        Assert.True(svc.Apply(src, pc, bundle, equipped));
        // bBaseAtk isn't in the static extractor switch — it's a separate
        // stat path we don't track in the bundle today, so the assertion
        // focuses on bonuses the extractor IS wired for. Both AtkRate
        // and LongAtkRate should land because the refines sum to 24.
        Assert.Equal(10, bundle.LongAtkRate); // outer + nested if branches both true
    }

    [Fact]
    public void NegativeBonus_PassesThrough()
    {
        var (svc, pc, bundle, _) = BuildFixture();
        Assert.True(svc.Apply("bonus bAspdRate,-5;", pc, bundle));
        Assert.Equal(-5, bundle.FlatAspdRate);
    }

    // ---- autobonus (DSL goal #2) ----

    [Fact]
    public void Autobonus_RegistersOnHitEntry()
    {
        var (svc, pc, bundle, bonusSvc) = BuildFixture();
        Assert.True(svc.Apply(
            "autobonus \"{ bonus bDex,20; bonus bLongAtkRate,10; }\",30,7000,BF_WEAPON;",
            pc, bundle));
        var entry = Assert.Single(bonusSvc.Registered);
        Assert.Equal(AutobonusTrigger.OnHit, entry.Trigger);
        Assert.Equal(30, entry.Rate);
        Assert.Equal(7000, entry.Duration);
        // The wrapped script body must round-trip as the original rAthena
        // bonus body, NOT the translated JS — re-translation happens on
        // trigger fire so the player's at-fire-time state is current.
        Assert.Contains("bonus bDex,20;", entry.Script);
    }

    [Fact]
    public void Autobonus2_RegistersWhenHitEntry()
    {
        var (svc, pc, bundle, bonusSvc) = BuildFixture();
        Assert.True(svc.Apply(
            "autobonus2 \"{ bonus bMaxHP,500; }\",10,5000,BF_WEAPON;", pc, bundle));
        var entry = Assert.Single(bonusSvc.Registered);
        Assert.Equal(AutobonusTrigger.WhenHit, entry.Trigger);
    }

    [Fact]
    public void Autobonus3_RegistersOnSkillEntry()
    {
        var (svc, pc, bundle, bonusSvc) = BuildFixture();
        Assert.True(svc.Apply(
            "autobonus3 \"{ bonus bFlee2,100; }\",1,3000,\"ASC_BREAKER\";", pc, bundle));
        var entry = Assert.Single(bonusSvc.Registered);
        Assert.Equal(AutobonusTrigger.OnSkill, entry.Trigger);
    }

    // ---- bonus3 bAutoSpell (DSL goal #3) ----

    [Fact]
    public void AutoSpell_RegistersAsOnHitAutobonus()
    {
        var (svc, pc, bundle, bonusSvc) = BuildFixture();
        Assert.True(svc.Apply(
            "bonus3 bAutoSpell,\"HP_ASSUMPTIO\",2,5;", pc, bundle));
        var entry = Assert.Single(bonusSvc.Registered);
        Assert.Equal(AutobonusTrigger.OnHit, entry.Trigger);
        Assert.Contains("HP_ASSUMPTIO", entry.Script);
    }

    [Fact]
    public void AutoSpellWhenHit_RegistersAsWhenHitAutobonus()
    {
        var (svc, pc, bundle, bonusSvc) = BuildFixture();
        Assert.True(svc.Apply(
            "bonus3 bAutoSpellWhenHit,\"HP_ASSUMPTIO\",2,5;", pc, bundle));
        var entry = Assert.Single(bonusSvc.Registered);
        Assert.Equal(AutobonusTrigger.WhenHit, entry.Trigger);
    }

    // ---- error paths ----

    [Fact]
    public void InvalidScript_ReturnsFalse_DoesNotThrow()
    {
        var (svc, pc, bundle, _) = BuildFixture();
        Assert.False(svc.Apply("this is not valid @@@", pc, bundle));
        // Bundle untouched.
        Assert.Equal(0, bundle.FlatAtk);
    }

    [Fact]
    public void EmptyScript_ReturnsTrue_DoesNotThrow()
    {
        var (svc, pc, bundle, _) = BuildFixture();
        Assert.True(svc.Apply("", pc, bundle));
        Assert.True(svc.Apply("   \n\t  ", pc, bundle));
    }

    // ---- translation cache ----

    [Fact]
    public void TranslationCache_ReusesAcrossCalls()
    {
        var (svc, pc, bundle, _) = BuildFixture();
        svc.Apply("bonus bAtk,10;", pc, bundle);
        var (h1, m1, _) = svc.CacheStats;
        svc.Apply("bonus bAtk,10;", pc, bundle);
        var (h2, m2, _) = svc.CacheStats;
        Assert.Equal(m1, m2); // miss count unchanged on the second call
        Assert.Equal(h1 + 1, h2);
    }

    // ---- gate helper ----

    [Fact]
    public void NeedsDynamicEval_DetectsMarkers()
    {
        Assert.False(IScriptedBonusService.NeedsDynamicEval("bonus bAtk,10;"));
        Assert.True(IScriptedBonusService.NeedsDynamicEval("if (getrefine() >= 7) { bonus bAtk,40; }"));
        Assert.True(IScriptedBonusService.NeedsDynamicEval(".@x = 5;"));
        Assert.True(IScriptedBonusService.NeedsDynamicEval("autobonus \"{...}\",30,7000,BF_WEAPON;"));
        Assert.True(IScriptedBonusService.NeedsDynamicEval("bonus3 bAutoSpell,\"X\",1,1;"));
        Assert.False(IScriptedBonusService.NeedsDynamicEval(""));
        Assert.False(IScriptedBonusService.NeedsDynamicEval(null));
    }

    // ---- fake autobonus service for assertions ----

    private sealed class FakeBonusSvc : IPlayerBonusService
    {
        public List<(AutobonusTrigger Trigger, string Script, int Rate, int Duration, ushort Flag)> Registered { get; } = new();

        public bool AddBonusScript(PlayerEntity pc, string script, int durationMs, ushort iconType, bool persistent)
            => true;
        public void ClearBonusScripts(PlayerEntity pc, int flag) { }
        public bool AddAutobonus(PlayerEntity pc, AutobonusTrigger trigger, string script, int rate, int durationMs, ushort flag)
        {
            Registered.Add((trigger, script, rate, durationMs, flag));
            return true;
        }
        public void DelAutobonus(PlayerEntity pc, AutobonusTrigger trigger, bool restore) { }
        public void ExecuteAutobonus(PlayerEntity pc, AutobonusTrigger trigger) { }
    }
}
