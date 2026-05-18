namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerFloatingNpc({ ... })</c> call. Floating NPCs have no world
/// position and are not placed in <c>IEntityRegistry</c>; they exist only as
/// event-handler bundles, looked up by name for Phase 5's <c>doevent</c>
/// dispatch.
/// </summary>
public sealed record FloatingNpcRegistration
{
    public required string Name { get; init; }
    public required NpcHooks Hooks { get; init; }
}
