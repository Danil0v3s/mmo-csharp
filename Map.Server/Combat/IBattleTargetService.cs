using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Read-mostly target / unit helpers from rAthena battle.cpp.
/// Shared by skill resolvers, mob AI, and item effects — every
/// caller used to grep block_list directly; this service is the
/// canonical entry point.
///
/// rAthena line refs:
/// <list type="bullet">
///   <item><c>battle_gettarget</c> — battle.cpp:9230</item>
///   <item><c>battle_gettargeted</c> — battle.cpp:9255</item>
///   <item><c>battle_getenemy</c> — battle.cpp:9280</item>
///   <item><c>battle_get_master</c> — battle.cpp:9300</item>
///   <item><c>battle_getcurrentskill</c> — battle.cpp:9335</item>
///   <item><c>battle_check_undead</c> — battle.cpp:9360</item>
///   <item><c>battle_check_coma</c> — battle.cpp:9385</item>
///   <item><c>is_infinite_defense</c> — battle.cpp:9410</item>
/// </list>
/// </summary>
public interface IBattleTargetService
{
    /// <summary>The id of the entity <paramref name="bl"/> is currently attacking (0 = none).</summary>
    int GetTarget(Entity bl);

    /// <summary>Entities currently targeting <paramref name="target"/>.</summary>
    IEnumerable<Entity> GetTargeted(Entity target);

    /// <summary>
    /// Nearest enemy within <paramref name="range"/> matching
    /// <paramref name="bl"/>'s allegiance. Returns null when no
    /// valid enemy is in sight.
    /// </summary>
    Entity? GetEnemy(Entity bl, int type, int range);

    /// <summary>Master / owner entity (pet, homun, merc, slave mob).</summary>
    Entity? GetMaster(Entity bl);

    /// <summary>Skill id this entity is currently casting (0 = none).</summary>
    ushort GetCurrentSkill(Entity bl);

    /// <summary>
    /// rAthena <c>battle_check_undead</c>: true if the entity is
    /// effectively undead (race == Undead OR defense element == Undead).
    /// </summary>
    bool CheckUndead(Status.BattleRace race, Status.BattleElement defenseElement);

    /// <summary>
    /// rAthena <c>battle_check_coma</c>: roll the coma proc against
    /// <paramref name="target"/>. Returns true if coma applied.
    /// </summary>
    bool CheckComa(Entity src, Entity target);

    /// <summary>True if the target's defense is effectively infinite (Steel Body etc.).</summary>
    bool IsInfiniteDefense(Entity target, BattleAttackType type);
}
