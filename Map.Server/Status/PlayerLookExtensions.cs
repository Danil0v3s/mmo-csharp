using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Higher-level helpers that compose on <see cref="IPlayerLookService"/>.
/// rAthena <c>pc_disguise</c> (pc.cpp:1099) — turn the PC into a
/// monster / other-class sprite by sending <c>LOOK_BASE</c> with the
/// class id.
/// </summary>
public static class PlayerLookExtensions
{
    /// <summary>
    /// Disguise as <paramref name="classId"/> (mob class id or pc job
    /// class id). Sends LOOK_BASE; the client renders the new sprite
    /// and keeps the disguise until either party-recall, warp,
    /// disconnect, or <see cref="Undisguise"/>.
    /// </summary>
    public static void Disguise(this IPlayerLookService look, PlayerEntity pc, int classId)
    {
        look.ChangeLook(pc, LookType.Base, (ushort)classId);
    }

    /// <summary>
    /// Cancel any active disguise. rAthena <c>pc_undisguise</c> just
    /// re-broadcasts the original class — we mirror via LOOK_BASE with
    /// the PC's natural class id (stored on the session's CharacterData).
    /// </summary>
    public static void Undisguise(this IPlayerLookService look, PlayerEntity pc, int restoreClassId)
    {
        look.ChangeLook(pc, LookType.Base, (ushort)restoreClassId);
    }
}
