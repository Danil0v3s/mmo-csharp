using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

/// <summary>
/// CRUD for per-character hotkey slots. Mirrors rAthena's hotkey table
/// (db/*/sql-files/main.sql · <c>hotkey</c>). Save semantics follow
/// rAthena <c>mmo_char_tosql</c>'s DELETE-all + INSERT-non-zero pattern
/// — see <see cref="ReplaceAsync"/>.
/// </summary>
public interface IHotkeyRepository
{
    Task<IReadOnlyList<HotkeyEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);

    /// <summary>
    /// Replace all hotkeys for <paramref name="charId"/>. Mirrors rAthena's
    /// <c>DELETE WHERE char_id = ?</c> followed by an INSERT for every
    /// entry whose <c>type</c> != 0 (zero-type rows are "empty slot" and
    /// not stored).
    /// </summary>
    Task ReplaceAsync(int charId, IEnumerable<HotkeyEntity> hotkeys, CancellationToken ct = default);
}
