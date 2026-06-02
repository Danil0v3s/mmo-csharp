namespace Map.Server.Status;

/// <summary>
/// Runtime battle status block attached to every fighting entity. Mirrors
/// rAthena <c>struct status_data</c> (status.hpp:3328) field-for-field
/// (renewal subset). Owned by <see cref="IStatusCalcService"/> — gameplay
/// code reads, the calc service writes during <c>status_calc_pc</c> /
/// <c>status_calc_mob</c> equivalents.
///
/// All fields are clamped exactly as rAthena does (<c>cap_value</c> at
/// SHRT_MAX / USHRT_MAX boundaries); call sites that mutate stats must
/// re-run the calc service so derived stats (hit/flee/cri/def2/mdef2/batk)
/// stay coherent.
/// </summary>
public sealed class BattleStats
{
    // --- Vital pools (rAthena status_data.hp / sp / max_hp / max_sp) ---
    public int Hp;
    public int Sp;
    public int MaxHp;
    public int MaxSp;

    // --- Primary stats (rAthena status_data.str / agi / vit / int_ / dex / luk) ---
    public short Str;
    public short Agi;
    public short Vit;
    public short IntStat;
    public short Dex;
    public short Luk;

    // --- Trait stats (renewal 4th-class). status_data.pow / sta / wis / spl / con / crt ---
    public short Pow;
    public short Sta;
    public short Wis;
    public short Spl;
    public short Con;
    public short Crt;

    /// <summary>
    /// COMBAT-10 — indexed access to the 12 primary + trait stats in rAthena
    /// <c>PARAM_*</c> order (0=Str 1=Agi 2=Vit 3=Int 4=Dex 5=Luk 6=Pow 7=Sta
    /// 8=Wis 9=Spl 10=Con 11=Crt). Used by the base→final param layering so
    /// <c>CalcPc</c> and <c>PlayerEntity.ShiftFinalParam</c> can loop the
    /// stats without a 12-way switch at every call site.
    /// </summary>
    public short this[int i]
    {
        get => i switch
        {
            0 => Str, 1 => Agi, 2 => Vit, 3 => IntStat, 4 => Dex, 5 => Luk,
            6 => Pow, 7 => Sta, 8 => Wis, 9 => Spl, 10 => Con, 11 => Crt,
            _ => 0,
        };
        set
        {
            switch (i)
            {
                case 0: Str = value; break;
                case 1: Agi = value; break;
                case 2: Vit = value; break;
                case 3: IntStat = value; break;
                case 4: Dex = value; break;
                case 5: Luk = value; break;
                case 6: Pow = value; break;
                case 7: Sta = value; break;
                case 8: Wis = value; break;
                case 9: Spl = value; break;
                case 10: Con = value; break;
                case 11: Crt = value; break;
            }
        }
    }

    // --- Weapon ATK (renewal: status_data.watk + watk2 split is in WeaponAtk) ---
    public ushort Batk;
    public ushort WatkMin;     // status_data.rhw.atk
    public ushort WatkMax;     // status_data.rhw.atk2
    public byte WeaponElement; // status_data.rhw.ele
    public byte WeaponLevel;   // rhw weapon level (1-5); drives the renewal PC atkmin DEX floor

    // COMBAT-18 — left-hand weapon (dual wield). Mirrors status_data.lhw.
    // Zero when no off-hand weapon is equipped (single weapon / shield), which
    // keeps BattleDamage.Damage2 at 0 (is_attack_left_handed false).
    public ushort LeftWatkMin;     // status_data.lhw.atk
    public ushort LeftWatkMax;     // status_data.lhw.atk2
    public byte LeftWeaponElement; // status_data.lhw.ele
    public byte LeftWeaponLevel;   // lhw weapon level (1-5)
    public int LeftWeaponType;     // lhw W_* type (0 = none/fist)
    public bool HasShield;         // COMBAT-29 — off-hand shield (drives shield ASPD base)

    public ushort MatkMin;
    public ushort MatkMax;

    // --- Speeds & timings (status_data.speed / amotion / adelay / dmotion) ---
    public ushort Speed;
    public ushort Amotion;       // attack motion (animation); ASPD-derived
    public ushort ClientAmotion; // client-side amotion (display)
    public ushort Adelay;        // attack delay (between swings)
    public ushort Dmotion;       // damage motion

    // --- Combat numbers (status_data.hit / flee / cri / flee2 / def2 / mdef2) ---
    public short Hit;
    public short Flee;
    public short Cri;     // stored at 10× display (rAthena convention)
    public short Flee2;   // stored at 10× display
    public short Def;
    public short Def2;    // soft def
    public short Mdef;
    public short Mdef2;   // soft mdef
    public short AspdRate;

    // --- Renewal trait derivatives (status_data.patk / smatk / res / mres / hplus / crate) ---
    public short Patk;
    public short Smatk;
    public short Res;
    public short Mres;
    public short Hplus;
    public short Crate;

    // --- Identity (status_data.def_ele / ele_lv / size / race / class_) ---
    public BattleElement DefenseElement = BattleElement.Neutral;
    public byte ElementLevel = 1;
    public BattleSize Size = BattleSize.Medium;
    public BattleRace Race = BattleRace.Formless;

    /// <summary>Mob behavior flags (status_data.mode). Players keep MD_NONE.</summary>
    public MobMode Mode = MobMode.None;

    /// <summary>Cell-distance attack range. PCs: weapon range; mobs: mob_db AttackRange.</summary>
    public short AttackRange = 1;
}
