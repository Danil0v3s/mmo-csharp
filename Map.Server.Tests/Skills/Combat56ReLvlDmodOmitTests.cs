using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-56 — per-arm RE_LVL_DMOD: skills whose rAthena ratio/misc arm OMITS the
/// renewal level-damage macro must NOT scale above level 99. The omit set
/// (<see cref="ReLvlDmodOmit"/>) gates the weapon (SkillImpl), magic and misc
/// (BattleCalculator) scaling paths.
/// </summary>
public class Combat56ReLvlDmodOmitTests
{
    [Theory]
    [InlineData(SkillIds.SM_BASH, true)]        // omits the macro
    [InlineData(SkillIds.AS_SONICBLOW, true)]   // omits
    [InlineData(SkillIds.MO_EXTREMITYFIST, true)] // Asura omits
    [InlineData(SkillIds.RK_SONICWAVE, false)]  // uses the macro
    [InlineData(SkillIds.LK_SPIRALPIERCE, false)]
    public void OmitsRatioScaling_matches_the_rathena_set(ushort skillId, bool omits)
        => Assert.Equal(omits, ReLvlDmodOmit.OmitsRatioScaling(skillId));

    [Theory]
    [InlineData(SkillIds.GS_GROUNDDRIFT, true)]
    [InlineData(SkillIds.HT_PHANTASMIC, true)]
    [InlineData(SkillIds.RA_CLUSTERBOMB, false)] // trap path uses RE_LVL_TMDMOD (COMBAT-55), not the misc omit set
    public void OmitsMiscScaling_matches_the_rathena_set(ushort skillId, bool omits)
        => Assert.Equal(omits, ReLvlDmodOmit.OmitsMiscScaling(skillId));

    [Fact]
    public void Magic_omit_skill_is_flat_above_99_while_non_omit_scales()
    {
        var calc = new BattleCalculator();
        var src = MakeMob(2001, level: 99);
        var tgt = MakeMob(2002, level: 1);
        src.Stats.MatkMin = src.Stats.MatkMax = 100;
        tgt.Stats.Mdef = 0; tgt.Stats.Mdef2 = 0;

        // Non-omit magic id (0) scales; an omit id stays flat.
        var omitId = SkillIds.NPC_FIREBREATH; // in the ratio-omit set
        var flat99 = calc.CalcMagicAttack(src, tgt, omitId, 1, 100).Damage;
        var scale99 = calc.CalcMagicAttack(src, tgt, 0, 1, 100).Damage;
        src.Level = 200;
        var flat200 = calc.CalcMagicAttack(src, tgt, omitId, 1, 100).Damage;
        var scale200 = calc.CalcMagicAttack(src, tgt, 0, 1, 100).Damage;

        Assert.Equal(flat99, flat200);          // omit → no >99 scaling
        Assert.Equal(scale99 * 2, scale200);    // non-omit → ×200/100
    }

    [Fact]
    public void Misc_omit_skill_does_not_get_the_above99_scaling()
    {
        var calc = new BattleCalculator();
        var src = MakeMob(2001, level: 200);
        var tgt = MakeMob(2002, level: 1);
        tgt.Stats.Def = 0; tgt.Stats.Def2 = 0;

        // The misc base (source.Level + INT) is level-dependent, so isolate the
        // RE_LVL_MDMOD by comparing omit vs non-omit at the SAME level 200: the
        // non-omit skill gets the extra ×200/100, the omit one does not.
        var omitDmg = calc.CalcMiscAttack(src, tgt, SkillIds.GS_GROUNDDRIFT, 1, 100).Damage;
        var nonOmitDmg = calc.CalcMiscAttack(src, tgt, 0, 1, 100).Damage;
        Assert.Equal(omitDmg * 200 / 100, nonOmitDmg);
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
