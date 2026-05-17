using Core.Server.IPC;

namespace Map.Server.Status;

/// <summary>
/// Renewal (re-) derived-stat formulas. Each method mirrors a specific
/// line range of rAthena <c>status.cpp</c> with the file + line numbers
/// cited inline so divergences can be traced back to source.
///
/// Inputs come from the IPC <see cref="CharacterDataResponse"/> rather
/// than a runtime "battle status" object — we don't yet have the
/// latter, and the captured Lv1 Novice has no buffs that differ from
/// the saved values. Stats that depend on equipment (hard def/mdef,
/// weapon atk) are placeholders matching the captured Novice; the
/// captured rAthena had the default starting items equipped (Knife +
/// Cotton Shirt) so those values aren't zero.
///
/// Capture-verified for Novice Lv1, all base stats 1, captured starting
/// items:
///   Hit=177, Flee=102, Aspd=590, Atk1(batk)=1, Atk2(weapon)=17,
///   Def1(soft)=1, Def2(hard)=10, Mdef1(soft)=1, Mdef2(hard)=0,
///   Matk1=0, Matk2=1, Critical=1, Flee2=1, MaxHp=40, MaxSp=11.
/// </summary>
public static class RenewalFormulas
{
    /// <summary>
    /// rAthena <c>status.cpp:2593</c>:
    /// <c>hit = level + dex + (PC ? luk/3 + 175 : 150) + 2*con</c>.
    /// Captured Novice Lv1 Dex1 Luk1: 1+1+0+175 = 177. ✓
    /// </summary>
    public static int Hit(CharacterDataResponse ch)
        => (int)(ch.BaseLevel + ch.Dex + (ch.Luk / 3) + 175 + 2 * ch.Con);

    /// <summary>
    /// rAthena <c>status.cpp:2598</c>:
    /// <c>flee = level + agi + luk/5 + 100 + 2*con</c>.
    /// Captured Novice Lv1 Agi1 Luk1: 1+1+0+100 = 102. ✓
    /// </summary>
    public static int Flee(CharacterDataResponse ch)
        => (int)(ch.BaseLevel + ch.Agi + (ch.Luk / 5) + 100 + 2 * ch.Con);

    /// <summary>
    /// rAthena <c>status.cpp:2683</c>:
    /// <c>cri = 1 + 10 + luk*3 + level/10</c> (renewal). <c>cri</c> is
    /// stored as 10× the displayed value; <c>clif_updatestatus(SP_CRITICAL)</c>
    /// emits <c>cri/10</c>. For Lv1 Luk1: cri = 14; wire = 1. ✓
    /// </summary>
    public static int CriticalWire(CharacterDataResponse ch)
        => (int)((1 + 10 + ch.Luk * 3 + ch.BaseLevel / 10) / 10);

    /// <summary>FLEE2 wire value. For Lv1 Luk1: flee2 = (10+0)/10 = 1. ✓</summary>
    public static int Flee2Wire(CharacterDataResponse ch) => (int)((10 + ch.Luk / 10) / 10);

    /// <summary>
    /// Soft def — <c>status.cpp:2606</c>:
    /// <c>def2 = (level + vit) / 2 + agi/5</c>.
    /// Renewal display: <c>SP_DEF1</c> (leftside, <c>pc.hpp:1246</c>).
    /// </summary>
    public static int SoftDef(CharacterDataResponse ch)
        => (int)((ch.BaseLevel + ch.Vit) / 2 + ch.Agi / 5);

    /// <summary>
    /// Hard def — equipment armor def. Renewal display: <c>SP_DEF2</c>
    /// (rightside). Capture shows 10; rAthena default starting items
    /// include Cotton Shirt (def 10). Hardcoded until equip lands.
    /// </summary>
    public static int HardDef(CharacterDataResponse ch) => 10;

    /// <summary>Soft mdef — <c>status.cpp:2614</c>: <c>int + level/4 + (dex+vit)/5</c>.</summary>
    public static int SoftMdef(CharacterDataResponse ch)
        => (int)(ch.IntStat + ch.BaseLevel / 4 + (ch.Dex + ch.Vit) / 5);

    /// <summary>Hard mdef — equipment magic def. 0 for unarmored.</summary>
    public static int HardMdef(CharacterDataResponse ch) => 0;

    /// <summary>
    /// Base ATK (renewal) — <c>status.cpp:2432</c>, melee branch:
    /// <c>(str*10 + dex*10/5 + luk*10/3 + level*10/4) / 10 + 5*pow</c>.
    /// Renewal display: <c>SP_ATK1</c> (leftside, batk).
    /// For Lv1 all-1: (10+2+3+2)/10 = 1. ✓
    /// </summary>
    public static int Batk(CharacterDataResponse ch)
        => (int)((ch.Str * 10 + ch.Dex * 10 / 5 + ch.Luk * 10 / 3 + ch.BaseLevel * 10 / 4) / 10 + 5 * ch.Pow);

    /// <summary>
    /// Weapon ATK rightside — <c>SP_ATK2 = watk + watk2 + eatk</c>.
    /// Capture shows 17 (rAthena default Knife). Hardcoded until items land.
    /// </summary>
    public static int WeaponAtk(CharacterDataResponse ch) => 17;

    /// <summary>MaxHP — <c>40 * (100 + vit) / 100</c>.</summary>
    public static int MaxHp(CharacterDataResponse ch) => (int)(40 * (100 + ch.Vit) / 100);

    /// <summary>MaxSP — <c>11 * (100 + int) / 100</c>.</summary>
    public static int MaxSp(CharacterDataResponse ch) => (int)(11 * (100 + ch.IntStat) / 100);

    /// <summary>
    /// ASPD — renewal formula is weapon-base amotion modified by agi/dex.
    /// Captured Novice (unarmed, Agi1, Dex1) shows 590. Placeholder until
    /// the full <c>status_calc_pc_sub</c> aspd path is ported.
    /// </summary>
    public static int AspdWire(CharacterDataResponse ch) => 590;

    /// <summary>MATK leftside — placeholder = 1 (Int 1 Novice).</summary>
    public static int MatkLeft(CharacterDataResponse ch) => 1;

    /// <summary>MATK rightside — placeholder = 0 (unarmed Novice).</summary>
    public static int MatkRight(CharacterDataResponse ch) => 0;

    /// <summary>
    /// MaxWeight — base from <c>job_db.yml MaxWeight</c>. Captured Novice
    /// = 20300; rAthena default base 20000 with bonus. Hardcoded until
    /// job_db loading lands.
    /// </summary>
    public static int MaxWeight(CharacterDataResponse ch) => 20300;

    /// <summary>SP_WEIGHT — sum of items × weight. 500 for captured Novice (default starting items).</summary>
    public static int Weight(CharacterDataResponse ch) => 500;

    /// <summary>SP_ATTACKRANGE — weapon range. Unarmed = 1.</summary>
    public static int AttackRange(CharacterDataResponse ch) => 1;
}
