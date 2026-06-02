using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Combat;

/// <summary>
/// Card / weapon-mastery damage modifiers — the rAthena
/// <c>battle_calc_cardfix</c> (battle.cpp:711) +
/// <c>battle_addmastery</c> (battle.cpp:2215) pair. Hooked into the
/// damage pipeline after the base ATK formula and before the floor
/// to 1 step.
///
/// <para>Cardfix accumulates per-card stat-mod bitsets sitting on
/// the attacker's equip aggregator (race, element, size, class,
/// race2). Mastery is a class-skill passive lookup (Demon Bane,
/// Beast Bane, Research, Madogear License, etc.).</para>
///
/// Both are big surface but most of the data — `add_mdmg`,
/// `indexed_bonus.*addrace*` — lives in rAthena's `pc_bonus*`
/// aggregator which we haven't ported yet. This service exposes
/// the canonical entry points so the damage pipeline doesn't need
/// to be rewritten when the aggregator lands. Mastery uses
/// <see cref="PlayerEntity.LearnedSkills"/> directly so it does
/// real work today.
/// </summary>
public interface IBattleCardService
{
    /// <summary>
    /// rAthena <c>battle_calc_cardfix</c>. Apply card-derived
    /// percent modifiers (race, element, size, class) to
    /// <paramref name="damage"/>. <paramref name="attackType"/>
    /// is the rAthena BF_WEAPON / BF_MAGIC / BF_MISC flag.
    /// <para>COMBAT-19: <paramref name="attackElement"/> overrides the
    /// attacker's element for the defender <c>bSubEle</c> lookup — magic/misc
    /// pass the resolved skill element; null falls back to the rh weapon
    /// element (correct for weapon attacks).</para>
    /// </summary>
    long CalcCardFix(BattleAttackType attackType, Entity src, Entity target, long damage, bool leftHand,
        BattleElement? attackElement = null);

    /// <summary>
    /// rAthena <c>battle_addmastery</c>. Add the attacker's
    /// weapon-mastery / race-bane passives to <paramref name="damage"/>.
    /// Renewal: returns the additive bonus only (caller adds);
    /// Pre-renewal: returns dmg + bonus. We follow renewal.
    /// </summary>
    long AddMastery(PlayerEntity attacker, Entity target, long damage, BattleAttackType type);

    /// <summary>
    /// rAthena <c>battle_calc_chorusbonus</c> (battle.cpp:2847). Counts
    /// MAPID_MINSTRELWANDERER (3rd-mask) members in the attacker's
    /// party on the same map. Returns 0 if &lt;3 members, 5 if &gt;7,
    /// else <c>members - 2</c>. Used by Wanderer / Minstrel chorus
    /// SCs (SC_DANCEWITHWUG, SC_SATURDAYNIGHTFEVER, SC_RICHMANKIM,
    /// etc.) to scale per-cast damage.
    ///
    /// <para><b>Renewal:</b> the rAthena function literally
    /// <c>return 0;</c> under <c>#ifdef RENEWAL</c>. The chorus
    /// damage matrix is pre-renewal only. Since our server runs in
    /// renewal mode, this is always 0 by parity — the entry point
    /// stays so a future #ifndef-RENEWAL fork has a clean home.</para>
    /// </summary>
    int CalcChorusBonus(PlayerEntity attacker);
}

/// <summary>
/// Mirror of rAthena's BF_* attack-type flags (battle.hpp).
/// Drives which card / mastery bonus branch fires.
/// </summary>
[Flags]
public enum BattleAttackType
{
    None   = 0x000,
    Weapon = 0x001, // BF_WEAPON
    Magic  = 0x002, // BF_MAGIC
    Misc   = 0x004, // BF_MISC
}
