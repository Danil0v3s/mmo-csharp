using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Port of rAthena <c>pc_jobchange</c> (pc.cpp:6750) — change the
/// PC's class. First slice handles the simple case: validate the
/// numeric class id, persist via session.CharacterData.Class,
/// broadcast LOOK_BASE so the sprite refreshes, recalc max HP/SP.
///
/// Out of scope for this slice (defer for follow-up):
/// <list type="bullet">
///   <item>pc_jobid2mapid / pc_mapid2jobid class-mask conversion
///         (upper/baby/expanded class trees).</item>
///   <item>Bard/Dancer auto-gender swap.</item>
///   <item>Skill tree reset / regrant for the new class.</item>
///   <item>Cart/Madogear/Riding cleanup if the new class can't use them.</item>
/// </list>
/// </summary>
public interface IJobChangeService
{
    /// <summary>
    /// Change <paramref name="pc"/>'s class to <paramref name="classId"/>.
    /// Returns false on validation failure (negative id, same-class
    /// no-op, session missing).
    /// </summary>
    bool Change(PlayerEntity pc, int classId);
}
