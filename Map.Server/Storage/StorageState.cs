namespace Map.Server.Storage;

/// <summary>
/// Per-session storage state. Mirrors rAthena <c>s_storage</c> trimmed
/// to the fields the gameplay path uses. Held on
/// <see cref="MapSessionData"/> while a kafra window is open and
/// committed via <see cref="IStorageService.Close"/>.
/// </summary>
public sealed class StorageState
{
    public bool IsOpen { get; set; }
    public bool Dirty { get; set; }
    public List<StorageItem> Items { get; } = new();
    /// <summary>Slot cap — default rAthena <c>MAX_STORAGE</c> = 600.</summary>
    public int MaxSlots { get; set; } = 600;
}

/// <summary>Outcome of an add/get attempt — mirrors rAthena <c>enum e_storage_add</c>.</summary>
public enum StorageOpResult
{
    Ok = 0,
    Invalid = 1,
    NoAccess = 2,
    NoRoom = 3,
    NotOpen = 4,
}
