using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Spawn;
using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// Pin renewal stat formulas against rAthena reference values. The
/// Novice Lv1 (all stats 1) baseline captured by
/// <see cref="Map.Server.Status.RenewalFormulas"/> is the parity anchor —
/// changes that drift here must trace back to a rAthena status.cpp diff.
/// </summary>
public class StatusCalcServiceTests
{
    [Fact]
    public void CalcPc_NoviceLv1_MatchesCaptureBaseline()
    {
        var calc = new StatusCalcService();
        var player = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), mapId: 0, x: 0, y: 0);

        calc.CalcPc(player, new PcBaseInputs(
            BaseLevel: 1, JobLevel: 1,
            Str: 1, Agi: 1, Vit: 1, Int: 1, Dex: 1, Luk: 1,
            Pow: 0, Sta: 0, Wis: 0, Spl: 0, Con: 0, Crt: 0,
            WeaponAtkMin: 17, WeaponAtkMax: 17,
            EquipDef: 10, EquipMdef: 0,
            AttackRange: 1));

        var s = player.Stats;
        // Hit = level + dex + luk/3 + 175 = 1+1+0+175 = 177  (status.cpp:2593)
        Assert.Equal(177, s.Hit);
        // Flee = level + agi + luk/5 + 100 = 1+1+0+100 = 102  (status.cpp:2598)
        Assert.Equal(102, s.Flee);
        // Def2 = (level+vit)/2 + agi/5 = 1+0 = 1               (status.cpp:2606)
        Assert.Equal(1, s.Def2);
        // Mdef2 = int + level/4 + (dex+vit)/5 = 1+0+0 = 1      (status.cpp:2614)
        Assert.Equal(1, s.Mdef2);
        // Cri (10×) = level/10 + 10 + luk*3 = 0+10+3 = 13      (status.cpp:2683)
        Assert.Equal(13, s.Cri);
        // Flee2 (10×) = luk + 10 = 11                          (status.cpp:2689)
        Assert.Equal(11, s.Flee2);
        // Batk = (10+2+3+2)/10 = 1                             (status.cpp:2432)
        Assert.Equal(1, s.Batk);
        // Weapon ATK passthrough
        Assert.Equal(17, s.WatkMin);
        Assert.Equal(17, s.WatkMax);
        // Hard def from equipment
        Assert.Equal(10, s.Def);
        // Race tagged Player_Human for the bonus tables
        Assert.Equal(BattleRace.PlayerHuman, s.Race);
        // MaxHP / MaxSP novice baseline
        Assert.Equal(40, s.MaxHp);
        Assert.Equal(11, s.MaxSp);
    }

    [Fact]
    public void CalcMob_PoringStats_HydratedFromDbEntry()
    {
        var calc = new StatusCalcService();
        var db = new MobDbEntry
        {
            Id = 1002,
            AegisName = "PORING",
            Name = "Poring",
            Level = 1, Hp = 50, Sp = 1,
            Attack = 7, Attack2 = 10,
            Defense = 0, MagicDefense = 5,
            Str = 6, Agi = 1, Vit = 1, Int = 0, Dex = 6, Luk = 5,
            AttackRange = 1, Size = "Small", Race = "Plant",
            Element = "Water", ElementLevel = 1,
            WalkSpeed = 400, AttackDelay = 1872, AttackMotion = 672,
            Modes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["CanMove"] = true,
                ["Looter"] = true,
                ["Assist"] = true,
                ["CanAttack"] = true,
            },
        };

        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var mob = new MobEntity(new EntityId(2000), db, origin, mapId: 0, x: 0, y: 0);
        calc.CalcMob(mob);

        var s = mob.Stats;
        Assert.Equal(50, s.MaxHp);
        Assert.Equal(50, s.Hp);
        Assert.Equal(BattleSize.Small, s.Size);
        Assert.Equal(BattleRace.Plant, s.Race);
        Assert.Equal(BattleElement.Water, s.DefenseElement);
        // mob_db ATK1..ATK2 map directly onto the watk min/max
        Assert.Equal(7, s.WatkMin);
        Assert.Equal(10, s.WatkMax);
        // Misc renewal — Hit (non-PC) = level + dex + 150
        Assert.Equal(1 + 6 + 150, s.Hit);
        // Flee (non-PC) = level + agi + 100
        Assert.Equal(1 + 1 + 100, s.Flee);
        // Mode parses string keys
        Assert.True((s.Mode & MobMode.CanMove) != 0);
        Assert.True((s.Mode & MobMode.Looter) != 0);
        Assert.False((s.Mode & MobMode.Aggressive) != 0);
        Assert.Equal(400, s.Speed);
    }
}
