using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Status;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleElementService"/>. Resolves the attack element
/// per rAthena <c>battle_get_weapon_element</c> / <c>battle_get_magic_element</c>
/// / <c>battle_get_misc_element</c> (battle.cpp:3477/3582/3675):
///
/// <list type="bullet">
///   <item>Weapon: the attacker's <c>Stats.WeaponElement</c> (which already
///         reflects an active weapon-endow SC — SC-02 / SC-11).</item>
///   <item>Magic: the skill_db declared element; <c>ELE_WEAPON</c> →
///         weapon element, <c>ELE_ENDOWED</c> → the endow element (we read it
///         off the weapon element, since endows update it in this engine),
///         <c>ELE_RANDOM</c> → a random element (Neutral..Undead).</item>
///   <item>Misc: same as magic except <c>ELE_WEAPON</c> / <c>ELE_ENDOWED</c>
///         collapse to Neutral (battle.cpp:3678).</item>
/// </list>
///
/// <para>The per-skill bespoke element overrides (Psychic Wave option element,
/// Adoramus+Ancilla → Neutral, Hell Inferno, dragon breath, spiritcharm,
/// arrow-element songs) are tracked in COMBAT-41; this resolver covers the base
/// <c>skill_get_ele</c> chain that the vast majority of bolts/traps use.</para>
/// </summary>
public sealed class BattleElementService : IBattleElementService
{
    private readonly ISkillDb? _db;
    private readonly Random _rng;

    public BattleElementService(ISkillDb? db = null, Random? rng = null)
    {
        _db = db;
        _rng = rng ?? Random.Shared;
    }

    public BattleElement GetWeaponElement(Entity attacker, ushort skillId, ushort skillLevel)
    {
        // rAthena: for a weapon skill, skill_get_ele may force an element; but
        // skill_id 0 (auto-attack) and ELE_WEAPON both take the weapon element.
        var declared = _db?.Get(skillId)?.Element ?? BattleElement.Neutral;
        if (skillId != 0 && IsConcrete(declared))
            return declared;
        return WeaponEle(attacker);
    }

    public BattleElement GetMagicElement(Entity attacker, ushort skillId, ushort skillLevel)
    {
        var declared = _db?.Get(skillId)?.Element ?? BattleElement.Neutral;
        return declared switch
        {
            BattleElement.Weapon => WeaponEle(attacker),  // ELE_WEAPON → rhw.ele
            BattleElement.Endowed => WeaponEle(attacker), // ELE_ENDOWED → endow (in WeaponElement)
            BattleElement.Random => RandomEle(),          // ELE_RANDOM → rnd()%ELE_ALL
            _ => declared,
        };
    }

    public BattleElement GetMiscElement(Entity attacker, ushort skillId, ushort skillLevel)
    {
        var declared = _db?.Get(skillId)?.Element ?? BattleElement.Neutral;
        return declared switch
        {
            // Misc attacks that would take the weapon/endow element are forced
            // Neutral (battle.cpp:3678 — Skotlex).
            BattleElement.Weapon or BattleElement.Endowed => BattleElement.Neutral,
            BattleElement.Random => RandomEle(),
            _ => declared,
        };
    }

    private static bool IsConcrete(BattleElement e) => e >= BattleElement.Neutral && e < BattleElement.Max;

    private static BattleElement WeaponEle(Entity attacker)
    {
        var el = (BattleElement)attacker.Stats.WeaponElement;
        return el == 0 ? BattleElement.Neutral : el;
    }

    // rAthena: element = rnd() % ELE_ALL → Neutral(0)..Undead(9).
    private BattleElement RandomEle() => (BattleElement)_rng.Next((int)BattleElement.All);
}
