using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Status;

/// <summary>
/// Recomputes <see cref="BattleStats"/> for an entity. Mirrors rAthena's
/// <c>status_calc_pc_</c> / <c>status_calc_mob_</c> / <c>status_calc_bl_</c>
/// chain — call after any input changes (equip, level, base-stat
/// allocation, SC apply/end) to rebuild derived fields (hit/flee/cri/
/// def2/mdef2/batk/aspd/maxhp/maxsp).
///
/// The MS3 first slice covers PC base stats + the renewal misc derivations
/// in <c>status_calc_misc</c>; equipment, skill bonuses, and SC modifiers
/// land as their owning subsystems port.
/// </summary>
public interface IStatusCalcService
{
    /// <summary>
    /// Rebuild <c>player.Stats</c> from base-stat + level inputs supplied
    /// by the caller (typically read from the char-side persistence
    /// payload at session enter). Preserves current HP/SP unless they
    /// exceed the new MaxHp / MaxSp.
    /// </summary>
    void CalcPc(PlayerEntity player, PcBaseInputs inputs);

    /// <summary>
    /// Rebuild <c>mob.Stats</c> from its <see cref="MobDbEntry"/>. Called
    /// once at spawn — mirrors <c>status_calc_mob_</c> (status.cpp:2731).
    /// </summary>
    void CalcMob(MobEntity mob);
}

/// <summary>
/// Inputs to <see cref="IStatusCalcService.CalcPc"/>. Mirrors the subset
/// of <c>map_session_data.status</c> that the renewal stat formulas
/// consume; everything else (equipment, SC, skill bonuses) is layered
/// on top by the calc service when those subsystems port.
/// </summary>
public readonly record struct PcBaseInputs(
    int BaseLevel,
    int JobLevel,
    int Str,
    int Agi,
    int Vit,
    int Int,
    int Dex,
    int Luk,
    int Pow,
    int Sta,
    int Wis,
    int Spl,
    int Con,
    int Crt,
    int WeaponAtkMin,
    int WeaponAtkMax,
    int EquipDef,
    int EquipMdef,
    int AttackRange,
    BattleElement WeaponElement = BattleElement.Neutral);
