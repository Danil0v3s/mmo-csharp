using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IMobRepository
{
    /// <summary>
    /// Gets a monster by its ID.
    /// </summary>
    Task<MobEntity?> GetByIdAsync(uint mobId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets a monster by its Aegis name.
    /// </summary>
    Task<MobEntity?> GetByAegisNameAsync(string aegisName, CancellationToken ct = default);
    
    /// <summary>
    /// Gets monsters by level range.
    /// </summary>
    Task<List<MobEntity>> GetByLevelRangeAsync(ushort minLevel, ushort maxLevel, CancellationToken ct = default);
    
    /// <summary>
    /// Gets monsters by race.
    /// </summary>
    Task<List<MobEntity>> GetByRaceAsync(string race, CancellationToken ct = default);
    
    /// <summary>
    /// Gets monsters by element.
    /// </summary>
    Task<List<MobEntity>> GetByElementAsync(string element, CancellationToken ct = default);
    
    /// <summary>
    /// Gets monsters by size.
    /// </summary>
    Task<List<MobEntity>> GetBySizeAsync(string size, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all MVP monsters.
    /// </summary>
    Task<List<MobEntity>> GetAllMvpsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Searches monsters by English name (partial match).
    /// </summary>
    Task<List<MobEntity>> SearchByNameAsync(string searchTerm, int limit = 50, CancellationToken ct = default);
    
    /// <summary>
    /// Gets monsters that drop a specific item.
    /// </summary>
    Task<List<MobEntity>> GetByDropItemAsync(string itemName, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all monsters (use with caution - large dataset).
    /// </summary>
    Task<List<MobEntity>> GetAllAsync(CancellationToken ct = default);
}