using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Status;

/// <summary>
/// Renewal port of rAthena's status_calc chain. Each method block has
/// the source line citation it mirrors so divergences can be audited.
///
/// Formula references all cap with <see cref="Cap"/>, which mirrors
/// rAthena's <c>cap_value</c> (utils.hpp). Renewal-only — pre-renewal
/// formulas are explicitly out of scope for this server (CLAUDE.md).
/// </summary>
public sealed class StatusCalcService : IStatusCalcService
{
    public void CalcPc(PlayerEntity player, PcBaseInputs inputs)
    {
        var s = player.Stats;
        player.Level = inputs.BaseLevel;

        s.Str = (short)inputs.Str;
        s.Agi = (short)inputs.Agi;
        s.Vit = (short)inputs.Vit;
        s.IntStat = (short)inputs.Int;
        s.Dex = (short)inputs.Dex;
        s.Luk = (short)inputs.Luk;
        s.Pow = (short)inputs.Pow;
        s.Sta = (short)inputs.Sta;
        s.Wis = (short)inputs.Wis;
        s.Spl = (short)inputs.Spl;
        s.Con = (short)inputs.Con;
        s.Crt = (short)inputs.Crt;

        // Weapon ATK rightside is the rhw {atk, atk2} pair. For PCs the
        // values come from the equipped weapon — status.cpp:2486.
        s.WatkMin = (ushort)Math.Max(0, inputs.WeaponAtkMin);
        s.WatkMax = (ushort)Math.Max(0, inputs.WeaponAtkMax);
        s.WeaponElement = (byte)inputs.WeaponElement;
        s.AttackRange = (short)Math.Max(1, inputs.AttackRange);

        // Hard def/mdef come from equipment; misc adds soft def2/mdef2.
        s.Def = (short)Math.Max(0, inputs.EquipDef);
        s.Mdef = (short)Math.Max(0, inputs.EquipMdef);
        s.Def2 = 0;
        s.Mdef2 = 0;
        s.Hit = 0;
        s.Flee = 0;
        s.Cri = 0;
        s.Flee2 = 0;
        s.Batk = 0;
        s.Patk = 0;
        s.Smatk = 0;
        s.Res = 0;
        s.Mres = 0;
        s.Hplus = 0;
        s.Crate = 0;

        CalcMisc(s, inputs.BaseLevel, isPc: true);

        // MaxHp / MaxSp — renewal job_db formula. Until job_db lands we
        // approximate with the Novice baseline scaled by Vit/Int + level.
        // status.cpp status_calc_maxhpsp_pc uses a job multiplier table; we
        // keep the Lv1 capture-verified output (40/11) intact at Lv1
        // and scale linearly per level until job_db wires in.
        var maxHp = NoviceMaxHp(inputs.BaseLevel, inputs.Vit);
        var maxSp = NoviceMaxSp(inputs.BaseLevel, inputs.Int);
        s.MaxHp = maxHp;
        s.MaxSp = maxSp;
        if (s.Hp <= 0 || s.Hp > maxHp) s.Hp = maxHp;
        if (s.Sp <= 0 || s.Sp > maxSp) s.Sp = maxSp;

        // Speed / amotion / adelay defaults — status.cpp:5990 status_calc_pc_
        // sets these from job_db. Placeholder uses captured Novice values.
        s.Speed = 150;
        s.Amotion = 590;
        s.ClientAmotion = 590;
        s.Adelay = 540;
        s.Dmotion = 480;

        s.Race = BattleRace.PlayerHuman;
        s.Size = BattleSize.Medium;
        s.DefenseElement = BattleElement.Neutral;
        s.ElementLevel = 1;
        s.Mode = MobMode.None;
    }

    public void CalcMob(MobEntity mob)
    {
        var db = mob.DbEntry;
        if (db == null) return;
        var s = mob.Stats;
        mob.Level = db.Level;

        s.Str = (short)db.Str;
        s.Agi = (short)db.Agi;
        s.Vit = (short)db.Vit;
        s.IntStat = (short)db.Int;
        s.Dex = (short)db.Dex;
        s.Luk = (short)db.Luk;

        // mob_db ATK1/ATK2 are the weapon-ATK min/max for non-PCs;
        // status.cpp:2481 picks them up as the watk range with no further
        // weapon variance.
        s.WatkMin = (ushort)Math.Max(0, db.Attack);
        s.WatkMax = (ushort)Math.Max(s.WatkMin, db.Attack2);

        s.Def = (short)Math.Clamp(db.Defense, 0, short.MaxValue);
        s.Mdef = (short)Math.Clamp(db.MagicDefense, 0, short.MaxValue);
        s.Res = (short)Math.Clamp(db.Resistance, 0, short.MaxValue);
        s.Mres = (short)Math.Clamp(db.MagicResistance, 0, short.MaxValue);

        s.Speed = (ushort)Math.Clamp(db.WalkSpeed > 0 ? db.WalkSpeed : 200, 1, ushort.MaxValue);
        s.Amotion = (ushort)Math.Clamp(db.AttackMotion > 0 ? db.AttackMotion : 1024, 1, ushort.MaxValue);
        s.Adelay = (ushort)Math.Clamp(db.AttackDelay > 0 ? db.AttackDelay : 1872, 1, ushort.MaxValue);
        s.Dmotion = (ushort)Math.Clamp(db.DamageMotion > 0 ? db.DamageMotion : 480, 1, ushort.MaxValue);
        s.ClientAmotion = (ushort)Math.Clamp(db.ClientAttackMotion > 0 ? db.ClientAttackMotion : s.Amotion, 1, ushort.MaxValue);

        s.AttackRange = (short)Math.Clamp(db.AttackRange, 1, short.MaxValue);

        s.Race = ParseRace(db.Race);
        s.Size = ParseSize(db.Size);
        s.DefenseElement = ParseElement(db.Element);
        s.ElementLevel = (byte)Math.Clamp(db.ElementLevel, 1, 4);
        s.Mode = ParseMode(db.Modes);

        s.MaxHp = db.Hp;
        if (s.Hp <= 0 || s.Hp > s.MaxHp) s.Hp = s.MaxHp;
        s.MaxSp = db.Sp;
        if (s.Sp <= 0 || s.Sp > s.MaxSp) s.Sp = s.MaxSp;

        // Reset derived stats so status_calc_misc rebuilds cleanly.
        s.Hit = 0; s.Flee = 0; s.Cri = 0; s.Flee2 = 0;
        s.Def2 = 0; s.Mdef2 = 0; s.Batk = 0;
        s.Patk = 0; s.Smatk = 0; s.Hplus = 0; s.Crate = 0;

        CalcMisc(s, db.Level, isPc: false);
    }

    /// <summary>
    /// Port of rAthena <c>status_calc_misc</c> (status.cpp:2552) renewal
    /// branch. Computes hit / flee / cri / flee2 / def2 / mdef2 /
    /// matk_min/max / batk from the primary stats already filled in.
    /// </summary>
    private static void CalcMisc(BattleStats s, int level, bool isPc)
    {
        // Hit: level + dex + (PC ? luk/3 + 175 : 150) + 2*con  — status.cpp:2593
        s.Hit = (short)CapShort(s.Hit + level + s.Dex + (isPc ? (s.Luk / 3 + 175) : 150) + 2 * s.Con, 1);
        // Flee: level + agi + (PC ? luk/5 : 0) + 100 + 2*con   — status.cpp:2598
        s.Flee = (short)CapShort(s.Flee + level + s.Agi + (isPc ? (s.Luk / 5) : 0) + 100 + 2 * s.Con, 1);

        // Def2 (soft): (level + vit) / 2 + (PC ? agi/5 : 0)    — status.cpp:2606
        s.Def2 = (short)CapShort(s.Def2 + (level + s.Vit) / 2 + (isPc ? (s.Agi / 5) : 0));
        // Mdef2 (soft) — status.cpp:2614
        s.Mdef2 = (short)(isPc
            ? CapShort(s.Mdef2 + s.IntStat + level / 4 + (s.Dex + s.Vit) / 5)
            : CapShort(s.Mdef2 + (s.IntStat + level) / 4));

        // PAtk / SMatk / Res / Mres / HPlus / CRate — status.cpp:2618..2640
        s.Patk = (short)CapShort(s.Patk + s.Pow / 3 + s.Con / 5);
        s.Smatk = (short)CapShort(s.Smatk + s.Spl / 3 + s.Con / 5);
        s.Res = (short)CapShort(s.Res + s.Sta + (s.Sta / 3) * 5);
        s.Mres = (short)CapShort(s.Mres + s.Wis + (s.Wis / 3) * 5);
        s.Hplus = (short)CapShort(s.Hplus + s.Crt);
        s.Crate = (short)CapShort(s.Crate + s.Crt / 3);

        // ATK for non-PCs: from rhw min/max set by CalcMob. PCs get watk
        // straight from equip — status.cpp:2644.

        // MATK min/max — status_base_matk_min/_max (status.cpp:2511..2542)
        if (isPc)
        {
            s.MatkMin = (ushort)CapUShort(s.IntStat + (s.IntStat / 2) + (s.Dex / 5) + (s.Luk / 3) + (level / 4) + 5 * s.Spl);
            s.MatkMax = s.MatkMin;
        }
        else
        {
            // mob path mixes weapon matk in (rhw.matk) — not in mob_db YAML
            // we currently load, so default = int + level only.
            s.MatkMin = (ushort)CapUShort(s.IntStat + level);
            s.MatkMax = (ushort)CapUShort(s.IntStat + level);
        }

        // Critical (10×) — status.cpp:2683
        s.Cri = (short)CapShort(s.Cri + (level / 10) + 10 + s.Luk * 3, 1);
        // Perfect flee (10×) — status.cpp:2689
        s.Flee2 = (short)CapShort(s.Flee2 + s.Luk + 10);

        // BAtk — status_base_atk (status.cpp:2379), PC and non-PC branches.
        s.Batk = (ushort)CapUShort(s.Batk + BaseAtk(s, level, isPc));
    }

    /// <summary>Port of <c>status_base_atk</c> (status.cpp:2379), renewal branch.</summary>
    private static int BaseAtk(BattleStats s, int level, bool isPc)
    {
        // Non-ranged weapons → str-leading. Ranged would swap str↔dex; the
        // renewal switch keys off PC weapon type which is not modeled yet,
        // so we default to melee until equip lands.
        var str = isPc
            ? (s.Str * 10 + s.Dex * 10 / 5 + s.Luk * 10 / 3 + level * 10 / 4) / 10 + 5 * s.Pow
            : s.Str + level;
        return Math.Max(0, str);
    }

    private static int NoviceMaxHp(int level, int vit)
        => Math.Max(1, 35 + level * 5) * (100 + vit) / 100;

    private static int NoviceMaxSp(int level, int intStat)
        => Math.Max(1, 10 + level) * (100 + intStat) / 100;

    private static int CapShort(int v, int min = 0)
        => v < min ? min : v > short.MaxValue ? short.MaxValue : v;

    private static int CapUShort(int v, int min = 0)
        => v < min ? min : v > ushort.MaxValue ? ushort.MaxValue : v;

    private static BattleRace ParseRace(string raceName) => raceName switch
    {
        "Formless" => BattleRace.Formless,
        "Undead" => BattleRace.Undead,
        "Brute" => BattleRace.Brute,
        "Plant" => BattleRace.Plant,
        "Insect" => BattleRace.Insect,
        "Fish" => BattleRace.Fish,
        "Demon" => BattleRace.Demon,
        "DemiHuman" or "Demihuman" => BattleRace.Demihuman,
        "Angel" => BattleRace.Angel,
        "Dragon" => BattleRace.Dragon,
        "Player_Human" => BattleRace.PlayerHuman,
        "Player_Doram" => BattleRace.PlayerDoram,
        _ => BattleRace.Formless,
    };

    private static BattleSize ParseSize(string sizeName) => sizeName switch
    {
        "Small" => BattleSize.Small,
        "Medium" => BattleSize.Medium,
        "Large" => BattleSize.Large,
        _ => BattleSize.Medium,
    };

    private static BattleElement ParseElement(string eleName) => eleName switch
    {
        "Neutral" => BattleElement.Neutral,
        "Water" => BattleElement.Water,
        "Earth" => BattleElement.Earth,
        "Fire" => BattleElement.Fire,
        "Wind" => BattleElement.Wind,
        "Poison" => BattleElement.Poison,
        "Holy" => BattleElement.Holy,
        "Dark" => BattleElement.Dark,
        "Ghost" => BattleElement.Ghost,
        "Undead" => BattleElement.Undead,
        _ => BattleElement.Neutral,
    };

    private static MobMode ParseMode(IReadOnlyDictionary<string, bool> modes)
    {
        var m = MobMode.None;
        if (modes.GetValueOrDefault("CanMove")) m |= MobMode.CanMove;
        if (modes.GetValueOrDefault("Looter")) m |= MobMode.Looter;
        if (modes.GetValueOrDefault("Aggressive")) m |= MobMode.Aggressive;
        if (modes.GetValueOrDefault("Assist")) m |= MobMode.Assist;
        if (modes.GetValueOrDefault("CastSensorIdle")) m |= MobMode.CastSensorIdle;
        if (modes.GetValueOrDefault("NoRandomWalk")) m |= MobMode.NoRandomWalk;
        if (modes.GetValueOrDefault("NoCast")) m |= MobMode.NoCast;
        if (modes.GetValueOrDefault("CanAttack")) m |= MobMode.CanAttack;
        if (modes.GetValueOrDefault("CastSensorChase")) m |= MobMode.CastSensorChase;
        if (modes.GetValueOrDefault("ChangeChase")) m |= MobMode.ChangeChase;
        if (modes.GetValueOrDefault("Angry")) m |= MobMode.Angry;
        if (modes.GetValueOrDefault("ChangeTargetMelee")) m |= MobMode.ChangeTargetMelee;
        if (modes.GetValueOrDefault("ChangeTargetChase")) m |= MobMode.ChangeTargetChase;
        if (modes.GetValueOrDefault("TargetWeak")) m |= MobMode.TargetWeak;
        if (modes.GetValueOrDefault("RandomTarget")) m |= MobMode.RandomTarget;
        if (modes.GetValueOrDefault("IgnoreMelee")) m |= MobMode.IgnoreMelee;
        if (modes.GetValueOrDefault("IgnoreMagic")) m |= MobMode.IgnoreMagic;
        if (modes.GetValueOrDefault("IgnoreRanged")) m |= MobMode.IgnoreRanged;
        if (modes.GetValueOrDefault("Mvp")) m |= MobMode.Mvp;
        if (modes.GetValueOrDefault("IgnoreMisc")) m |= MobMode.IgnoreMisc;
        if (modes.GetValueOrDefault("KnockbackImmune")) m |= MobMode.KnockbackImmune;
        if (modes.GetValueOrDefault("TeleportBlock")) m |= MobMode.TeleportBlock;
        if (modes.GetValueOrDefault("FixedItemDrop")) m |= MobMode.FixedItemDrop;
        if (modes.GetValueOrDefault("Detector")) m |= MobMode.Detector;
        if (modes.GetValueOrDefault("StatusImmune")) m |= MobMode.StatusImmune;
        if (modes.GetValueOrDefault("SkillImmune")) m |= MobMode.SkillImmune;
        return m;
    }
}
