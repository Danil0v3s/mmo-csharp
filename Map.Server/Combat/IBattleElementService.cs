using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Combat;

/// <summary>
/// rAthena element resolvers — picks the right element for an
/// outgoing weapon swing, magic skill, or misc-attack effect:
///
/// <list type="bullet">
///   <item><c>battle_get_weapon_element</c> (battle.cpp:5980) —
///         attacker's weapon element, optionally overridden by
///         elemental endow SCs.</item>
///   <item><c>battle_get_magic_element</c> (battle.cpp:6010) —
///         skill default element or its variable override.</item>
///   <item><c>battle_get_misc_element</c> (battle.cpp:6040) —
///         used by traps / item-effect damage.</item>
/// </list>
/// </summary>
public interface IBattleElementService
{
    BattleElement GetWeaponElement(Entity attacker, ushort skillId, ushort skillLevel);
    BattleElement GetMagicElement(Entity attacker, ushort skillId, ushort skillLevel);
    BattleElement GetMiscElement(Entity attacker, ushort skillId, ushort skillLevel);
}
