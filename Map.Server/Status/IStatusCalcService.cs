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

    /// <summary>
    /// ST.5 — rAthena <c>status_calc_homunculus_</c> (status.cpp:2858).
    /// Companions in the C# port are <see cref="MobEntity"/> instances
    /// (no dedicated HomunculusEntity class); delegates to
    /// <see cref="CalcMob"/> with an optional level override that
    /// IHomunculusService.RecvData supplies from the char-side payload.
    /// </summary>
    void CalcHomunculus(MobEntity homun, int levelOverride = 0);

    /// <summary>
    /// ST.5 — rAthena <c>status_calc_mercenary_</c> (status.cpp:2887).
    /// </summary>
    void CalcMercenary(MobEntity merc, int levelOverride = 0);

    /// <summary>
    /// ST.5 — rAthena <c>status_calc_elemental_</c> (status.cpp:2920).
    /// </summary>
    void CalcElemental(MobEntity ele, int levelOverride = 0);

    /// <summary>
    /// ST.8 — rAthena <c>status_calc_npc_</c> (status.cpp:2942). Most
    /// NPCs have no stats; this is a no-op when the NPC doesn't carry
    /// a MobDbEntry-style stat block (i.e. it's a dialog NPC). Boss-mode
    /// scripted NPCs that fight back hydrate via the optional stats
    /// block their script registrar declares.
    /// </summary>
    void CalcNpc(NpcEntity npc);
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
    BattleElement WeaponElement = BattleElement.Neutral,
    // DBR-1c: JobId + WeaponType drive the job_aspd_db lookup. JobId 0 =
    // Novice (rAthena default for char-create); WeaponType 0 = Fist /
    // bare-hand (rAthena enum weapon_type W_FIST). Both fall through to
    // the JobAspdCacheService.DefaultBaseAspdMs fallback when missing.
    int JobId = 0,
    int WeaponType = 0,
    // COMBAT-04: right-hand weapon level (1-5; 0 = bare-handed) for the
    // renewal PC base-atk DEX floor: atkmin = dex*(80+weaponLv*20)/100.
    int WeaponLevel = 0);
