using Map.Server.Entities;

namespace Map.Server.Handlers.Actions;

/// <summary>
/// One handler per CZ_REQUEST_ACTION action code (rAthena
/// <c>clif_parse_ActionRequest_sub</c>, clif.cpp:11671). Same strategy
/// shape as <c>ISkillResolver</c>: <see cref="ActionRegistry"/> indexes
/// by <see cref="ActionCode"/> and dispatches a single Apply call.
/// </summary>
public interface IActionHandler
{
    /// <summary>rAthena action byte (0 single-attack, 7 continuous, 2 sit, 3 stand, 1 pickup, etc).</summary>
    byte ActionCode { get; }

    void Apply(PlayerEntity player, int targetId);
}
