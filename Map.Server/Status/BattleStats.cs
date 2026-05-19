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

    // --- Weapon ATK (renewal: status_data.watk + watk2 split is in WeaponAtk) ---
    public ushort Batk;
    public ushort WatkMin;     // status_data.rhw.atk
    public ushort WatkMax;     // status_data.rhw.atk2
    public byte WeaponElement; // status_data.rhw.ele
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
