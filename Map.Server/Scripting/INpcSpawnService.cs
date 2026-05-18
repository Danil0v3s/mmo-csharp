namespace Map.Server.Scripting;

/// <summary>
/// Boot-time placement of every <see cref="Records.NpcRegistration"/> in the
/// registry as an <see cref="Entities.NpcEntity"/> in <see cref="Entities.IEntityRegistry"/>.
/// Mirrors <see cref="Spawn.IMobSpawnService.SpawnInitial"/> in style.
/// </summary>
public interface INpcSpawnService
{
    /// <summary>
    /// Walk the registry and add an <c>NpcEntity</c> per record. NPCs on
    /// unhosted maps are skipped with a warning. Floating NPCs are
    /// intentionally skipped — they live in the registry as event handlers,
    /// not world entities.
    /// </summary>
    void SpawnInitial();

    int SpawnedCount { get; }
    int SkippedUnknownMapCount { get; }
}
