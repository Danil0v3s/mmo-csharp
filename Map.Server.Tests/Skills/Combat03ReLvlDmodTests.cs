using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Swordman;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-03 — renewal base-level damage modifier (RE_LVL_DMOD,
/// config/const.hpp:94): above base level 99 the skill ratio is multiplied by
/// <c>baseLevel / divisor</c>. At/below 99 it's a no-op; divisor 0 disables.
/// </summary>
public class Combat03ReLvlDmodTests
{
    private const long Swing = 1000;

    private sealed class FixedSwingBattle : IBattleCalculator
    {
        public BattleDamage CalcWeaponAttack(Entity s, Entity t) => new() { Damage = Swing };
        public BattleDamage CalcMagicAttack(Entity s, Entity t, ushort i, ushort l, int r, long c = 0) => new() { Damage = Swing };
        public BattleDamage CalcMiscAttack(Entity s, Entity t, ushort i, ushort l, int r) => new() { Damage = Swing };
    }

    /// <summary>Fixed 200% ratio + configurable RE_LVL divisor, for isolating the modifier.</summary>
    private sealed class FixedRatioSkill : WeaponSkillImpl
    {
        private readonly int _div;
        public FixedRatioSkill(int div) : base(SkillIds.SM_BASH) => _div = div;
        public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort lv) => 200;
        protected override int ReLvlDivisor => _div;
    }

    private static (SkillExerciser ex, Map.Server.Skills.Behaviors.SkillBehaviorContext ctx) Fixed()
    {
        var ex = new SkillExerciser(family: "Swordman");
        return (ex, ex.Context with { Battle = new FixedSwingBattle() });
    }

    private static long LastDamage(SkillExerciser ex) =>
        ex.Recorder.Events.Where(e => e.Kind == "damage")
            .Select(e => (long)(int)e.Data["damage"]!).Last();

    [Theory]
    [InlineData(99, 100, 2000)]   // ≤99: no scaling → 1000 × 200%
    [InlineData(100, 100, 2000)]  // 100/100 = ×1 → unchanged
    [InlineData(200, 100, 4000)]  // 200/100 = ×2 → ratio 400% → 4000
    [InlineData(200, 0, 2000)]    // divisor 0 disables → unchanged
    [InlineData(200, 200, 2000)]  // 200/200 = ×1
    [InlineData(240, 120, 4000)]  // 240/120 = ×2
    public void ReLvlDmod_scales_ratio_above_99(int level, int divisor, long expected)
    {
        var (ex, ctx) = Fixed();
        ex.Caster.Level = level;
        new FixedRatioSkill(divisor).CastendDamageId(ex.Caster, ex.Target, 1, ctx);
        Assert.Equal(expected, LastDamage(ex));
    }

    [Fact]
    public void Bash_real_plugin_scales_at_level_175()
    {
        // Bash lv10 ratio = 400%. At lv99: 1000×400% = 4000. At lv175: ×175/100 → 7000.
        var (ex99, ctx99) = Fixed(); ex99.Caster.Level = 99;
        new Bash().CastendDamageId(ex99.Caster, ex99.Target, 10, ctx99);
        Assert.Equal(4000, LastDamage(ex99));

        var (ex175, ctx175) = Fixed(); ex175.Caster.Level = 175;
        new Bash().CastendDamageId(ex175.Caster, ex175.Target, 10, ctx175);
        Assert.Equal(4000L * 175 / 100, LastDamage(ex175)); // 7000
    }

    [Fact]
    public void Magic_path_scales_with_base_level_above_99()
    {
        var calc = new BattleCalculator(); // no _sc, no _cards
        var src = MakeMob(2001, level: 99);
        var tgt = MakeMob(2002, level: 1);
        src.Stats.MatkMin = src.Stats.MatkMax = 100;
        tgt.Stats.DefenseElement = BattleElement.Neutral; tgt.Stats.ElementLevel = 1;
        tgt.Stats.Mdef = 0; tgt.Stats.Mdef2 = 0;

        var d99 = calc.CalcMagicAttack(src, tgt, 0, 1, ratePerLevel: 100).Damage;

        src.Level = 200;
        var d200 = calc.CalcMagicAttack(src, tgt, 0, 1, ratePerLevel: 100).Damage;

        Assert.Equal(100, d99);   // no scaling at 99
        Assert.Equal(200, d200);  // ×200/100
    }

    private static MobEntity MakeMob(int id, int level)
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(id), db, origin, mapId: 0, x: 0, y: 0);
        m.Level = level;
        return m;
    }
}
