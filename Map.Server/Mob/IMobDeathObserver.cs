using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// FEATURE-01 — rAthena <c>mob_dead</c> fan-out hub (the quest / achievement / pet-catch / MVP
/// portion, steps 4–7). EXP + drops are already awarded upstream in
/// <c>DamageService.HandleDeath</c> / <c>MobSpawnService.KillMob</c>; this hub runs the remaining
/// subsystem notifications <b>while the mob (and its damage log) is still alive</b>, i.e. before
/// <c>KillMob</c> frees the entity.
/// </summary>
public interface IMobDeathObserver
{
    /// <summary>
    /// Dispatch a mob death. <paramref name="killer"/> is the last-hit PC (null for GM/scripted
    /// kills). <paramref name="dmgLog"/> is the snapshot of the mob's damage contributors taken
    /// before the entity is removed.
    /// </summary>
    void OnMobDead(MobEntity mob, PlayerEntity? killer, IReadOnlyList<MobDmgList.DmgEntry> dmgLog);
}
