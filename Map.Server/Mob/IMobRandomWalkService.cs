namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena <c>mob_randomwalk</c> (mob.cpp:1673). Per-tick
/// idle wander step: picks a cell ±7 from the mob's current
/// position and starts a walk. Gated by:
/// <list type="bullet">
///   <item><c>md->next_walktime &gt; tick</c> — not yet due.</item>
///   <item><c>MD_NORANDOMWALK</c> mode bit set — opted out.</item>
///   <item><c>!unit_can_move</c> — root / freeze / sleep / etc.</item>
///   <item><c>!MD_CANMOVE</c> — mob cannot move at all.</item>
/// </list>
/// </summary>
public interface IMobRandomWalkService
{
    /// <summary>rAthena MIN_RANDOMWALKTIME default (mob.hpp).</summary>
    public const int MinWalkIntervalMs = 4_000;

    /// <summary>Maximum wander offset from current cell (rAthena d=7).</summary>
    public const int MaxWanderRadius = 7;

    /// <summary>
    /// Try to pick a random nearby cell and start walking. Returns
    /// true if a walk was issued, false if the gate refused or no
    /// passable cell could be found.
    /// </summary>
    bool TryWander(Entities.MobEntity mob, long nowTick);
}
