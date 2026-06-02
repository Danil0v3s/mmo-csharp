using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Builds a <see cref="PcBaseInputs"/> from a player's <b>current</b> live state
/// (persisted BASE params + equip-derived weapon/def numbers + job/weapon type) so
/// any caller that needs an idempotent <c>status_calc_pc</c> recalc — level-up
/// (<see cref="ExpService"/>), death (<see cref="Map.Server.Combat.PcDeathService"/>),
/// etc. — re-folds equip + job-bonus + SC stat mods the same way. Reads
/// <see cref="PlayerEntity.BaseParams"/> (not the conflated final Stats.X) so the
/// recalc never strips the job bonus on the next pass (COMBAT-10).
/// </summary>
public static class PcRecalcInputs
{
    public static PcBaseInputs FromCurrent(PlayerEntity p) => new(
        BaseLevel: p.Level,
        JobLevel: p.JobLevel,
        Str: p.BaseParams.Str, Agi: p.BaseParams.Agi, Vit: p.BaseParams.Vit,
        Int: p.BaseParams.IntStat, Dex: p.BaseParams.Dex, Luk: p.BaseParams.Luk,
        Pow: p.BaseParams.Pow, Sta: p.BaseParams.Sta, Wis: p.BaseParams.Wis,
        Spl: p.BaseParams.Spl, Con: p.BaseParams.Con, Crt: p.BaseParams.Crt,
        WeaponAtkMin: p.Stats.WatkMin, WeaponAtkMax: p.Stats.WatkMax,
        EquipDef: p.Stats.Def, EquipMdef: p.Stats.Mdef,
        AttackRange: p.Stats.AttackRange,
        JobId: p.ClassId, WeaponType: p.WeaponType,
        LeftWeaponAtkMin: p.Stats.LeftWatkMin, LeftWeaponAtkMax: p.Stats.LeftWatkMax,
        LeftWeaponLevel: p.Stats.LeftWeaponLevel, LeftWeaponType: p.Stats.LeftWeaponType,
        LeftWeaponElement: (BattleElement)p.Stats.LeftWeaponElement,
        HasShield: p.Stats.HasShield);
}
