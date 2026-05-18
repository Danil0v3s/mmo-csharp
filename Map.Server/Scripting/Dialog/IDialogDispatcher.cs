using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

/// <summary>
/// Orchestrates the lifecycle of one player's dialog with one NPC. Lives
/// alongside the game loop: <c>ContactNpcHandler</c> kicks off a session,
/// resume handlers (CZ_REQ_NEXT_SCRIPT / CZ_CHOOSE_MENU / CZ_CLOSE_DIALOG)
/// advance it, the dispatcher sends the matching ZC packets per step.
///
/// Phase 2 wires <c>onClick</c> only — touch and timer hooks land in Phase 5.
/// </summary>
public interface IDialogDispatcher
{
    /// <summary>
    /// Player clicked an NPC. If the NPC has an <c>onClick</c> hook, build a
    /// fresh <see cref="DialogSession"/>, invoke the generator, and drive the
    /// first run of steps. Returns true if a dialog was started.
    /// </summary>
    bool StartOnClick(MapSessionData session, NpcEntity npc);

    /// <summary>Resume the dialog after the client clicked "Next" (CZ_REQ_NEXT_SCRIPT).</summary>
    void ResumeNext(MapSessionData session, uint npcId);

    /// <summary>Resume the dialog after the client picked a menu option (CZ_CHOOSE_MENU).</summary>
    void ResumeMenu(MapSessionData session, uint npcId, byte selection);

    /// <summary>Finalise the dialog after the client clicked "Close" (CZ_CLOSE_DIALOG).</summary>
    void ResumeClose(MapSessionData session, uint npcId);
}
