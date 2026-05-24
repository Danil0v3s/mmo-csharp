using Map.Server.Entities;

namespace Map.Server.Party;

/// <summary>
/// Port of rAthena <c>party_foreachsamemap</c> (party.cpp:1108) — the
/// "iterate party members on the same map" enumerator used by every
/// party-broadcast skill (Angelus, Magnificat, Suffragium, Impositio,
/// Gloria, Renovatio, Convenio, …) and by the party-XY map update
/// broadcasts.
///
/// <para>This is a foundation helper. Per-skill bodies must not walk
/// <see cref="IEntityRegistry.All"/> directly to find party members;
/// they route through this service so the eligibility check (alive,
/// same map, optional self-include) stays single-sourced.</para>
/// </summary>
public interface IPartyMapService
{
    /// <summary>
    /// Invoke <paramref name="callback"/> once per party-mate of
    /// <paramref name="origin"/> that is on the same map, alive, and
    /// matches <paramref name="includeSelf"/>. Returns the number of
    /// invocations.
    ///
    /// <para>If <paramref name="origin"/> has no party
    /// (<see cref="PlayerEntity.PartyId"/> &lt;= 0), the callback fires
    /// for <paramref name="origin"/> alone when <paramref name="includeSelf"/>
    /// is true; otherwise returns 0. This mirrors rAthena's behavior
    /// of treating a solo player as a "party of one" for buff-spread
    /// purposes — Angelus / Magnificat fire on the caster regardless.</para>
    /// </summary>
    int ForEachOnSameMap(PlayerEntity origin, Action<PlayerEntity> callback, bool includeSelf = true);

    /// <summary>
    /// Variant that filters by a max <paramref name="range"/> in cells
    /// from <paramref name="origin"/>'s position. <paramref name="range"/> &lt; 0
    /// = whole-map (same as <see cref="ForEachOnSameMap"/>). rAthena
    /// uses range=AREA_SIZE (14) for Pneuma-style "near" effects and
    /// range=-1 for true whole-map broadcasts.
    /// </summary>
    int ForEachOnSameMapInRange(PlayerEntity origin, short range, Action<PlayerEntity> callback, bool includeSelf = true);
}
