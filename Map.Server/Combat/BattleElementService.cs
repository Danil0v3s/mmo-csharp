using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleElementService"/>. Each resolver picks
/// the right element source per rAthena:
///
/// <list type="bullet">
///   <item>Weapon: attacker's <c>Stats.WeaponElement</c>, then look
///         for any element-endow SC override (none ported yet).</item>
///   <item>Magic: skill_db default element; skills with element
///         <c>ELE_WEAPON</c> fall back to the weapon resolver,
///         <c>ELE_ENDOWED</c> reads the endow SC, <c>ELE_RANDOM</c>
///         picks a random element.</item>
///   <item>Misc: same fallback chain as magic; traps default to
///         Neutral.</item>
/// </list>
///
/// Skill-element overrides need the skill_db element column; until
/// that ships, magic + misc return Neutral.
/// </summary>
public sealed class BattleElementService : IBattleElementService
{
    private static readonly Random Rng = new();

    public BattleElement GetWeaponElement(Entity attacker, ushort skillId, ushort skillLevel)
    {
        // rAthena: weapon element falls back to ELE_NEUTRAL when 0.
        var el = (BattleElement)attacker.Stats.WeaponElement;
        return el == 0 ? BattleElement.Neutral : el;
    }

    public BattleElement GetMagicElement(Entity attacker, ushort skillId, ushort skillLevel)
        => BattleElement.Neutral;

    public BattleElement GetMiscElement(Entity attacker, ushort skillId, ushort skillLevel)
        => BattleElement.Neutral;
}
