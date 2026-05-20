using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Side effects fired during the swing pipeline that aren't damage
/// computation per se: drain (HP/SP), ammo consumption, auto-cast
/// hooks, vanish (full pool drain), vellum (% MaxHP), and the
/// status-block check (SCs that abort damage entirely).
///
/// rAthena reference points: <c>battle_drain</c> (battle.cpp:6440),
/// <c>battle_consume_ammo</c> (battle.cpp:6580),
/// <c>battle_autocast_aftercast</c> (battle.cpp:6610),
/// <c>battle_autocast_elembuff_skill</c> (battle.cpp:6655),
/// <c>battle_vanish_damage</c> (battle.cpp:6800),
/// <c>battle_vellum_damage</c> (battle.cpp:6845),
/// <c>battle_status_block_damage</c> (battle.cpp:6890).
/// </summary>
public interface IBattleEffectsService
{
    /// <summary>HP/SP drain on hit (Hunter Bow card etc.).</summary>
    void Drain(PlayerEntity attacker, Entity target, long rDamage, long lDamage, int race, int classKind);

    /// <summary>Decrement ammo on a ranged attack.</summary>
    void ConsumeAmmo(PlayerEntity attacker, ushort skillId, ushort skillLevel);

    /// <summary>Auto-cast skill after a normal swing (Magnum proc, etc.).</summary>
    void AutocastAfterCast(PlayerEntity attacker, Entity target);

    /// <summary>Element-buff auto-cast (Flame Launcher etc.).</summary>
    void AutocastElemBuff(PlayerEntity attacker, ushort skillId);

    /// <summary>Vanishing-Buster / Soul Vanishing: drain a % of MaxHP/MaxSP.</summary>
    void VanishDamage(PlayerEntity attacker, Entity target, int hpPercent, int spPercent);

    /// <summary>Vellum series: fixed % of MaxHP damage that ignores defense.</summary>
    long VellumDamage(Entity target, int percentOfMaxHp);

    /// <summary>True if the target has an SC that blocks damage entirely (Steel Body etc.).</summary>
    bool StatusBlocksDamage(Entity target, BattleAttackType type);
}
