using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IItemRepository
{
    /// <summary>
    /// Gets an item by its ID.
    /// </summary>
    Task<ItemEntity?> GetByIdAsync(uint itemId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets an item by its Aegis name.
    /// </summary>
    Task<ItemEntity?> GetByAegisNameAsync(string aegisName, CancellationToken ct = default);
    
    /// <summary>
    /// Gets items by type.
    /// </summary>
    Task<List<ItemEntity>> GetByTypeAsync(string type, CancellationToken ct = default);
    
    /// <summary>
    /// Gets items by type and subtype.
    /// </summary>
    Task<List<ItemEntity>> GetByTypeAndSubtypeAsync(string type, string subtype, CancellationToken ct = default);
    
    /// <summary>
    /// Searches items by English name (partial match).
    /// </summary>
    Task<List<ItemEntity>> SearchByNameAsync(string searchTerm, int limit = 50, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all items (use with caution - large dataset).
    /// </summary>
    Task<List<ItemEntity>> GetAllAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets items within a price range.
    /// </summary>
    Task<List<ItemEntity>> GetByPriceRangeAsync(uint minPrice, uint maxPrice, CancellationToken ct = default);
    
    /// <summary>
    /// Gets equipment items for a specific equip location.
    /// </summary>
    Task<List<ItemEntity>> GetByEquipLocationAsync(string location, CancellationToken ct = default);
}