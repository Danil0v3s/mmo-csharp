using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// Common AI for owned summons — pets (pet.cpp), homunculi
/// (homunculus.cpp), mercenaries (mercenary.cpp), elementals
/// (elemental.cpp), and slave mobs (mob.cpp <c>mob_ai_sub_hard_slavemob</c>).
///
/// All four behave similarly: follow the master, optionally attack
/// the master's target, despawn when the master leaves the map. We
/// collapse the per-type variants into one service that reads
/// <see cref="Entity.MasterId"/> off any entity carrying one.
/// </summary>
public interface ISummonAiService
{
    /// <summary>Game-loop pump — drives follow + assist-attack for every summon.</summary>
    void Tick(long nowTick);
}
