using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Manages ground-placed skill effects ("skill units" in rAthena —
/// <c>skill_unitsetting</c> / <c>skill_unit_onplace_timer</c>, skill.cpp).
/// First slice: a per-cell periodic-damage model that fits Magnus
/// Exorcismus / Storm Gust / Sanctuary cleanly. Defensive units
/// (Safety Wall, Pneuma) layer on the same lifecycle once the
/// damage-interception hook lands.
/// </summary>
public interface ISkillUnitService
{
    /// <summary>
    /// Create a group at <paramref name="centerX"/>/<paramref name="centerY"/>.
    /// The cells covered come from the skill's layout (square radius for
    /// the starter set).
    /// </summary>
    SkillUnitGroup? Place(Entity caster, ushort skillId, ushort skillLevel, short centerX, short centerY);

    /// <summary>Game-loop pump — fires periodic effects, expires groups.</summary>
    void Tick(long nowTick);
}
