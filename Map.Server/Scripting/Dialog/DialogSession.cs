using Jint.Native;
using Jint.Native.Object;
using Map.Server.Entities;

namespace Map.Server.Scripting.Dialog;

/// <summary>
/// Per-player dialog state. Holds the running generator iterator, the bound
/// NPC, and the live <see cref="DialogContext"/>. One <see cref="DialogSession"/>
/// is stored on <see cref="Session.MapSessionData.Dialog"/> while the player
/// is in a dialog.
/// </summary>
public sealed class DialogSession
{
    public NpcEntity Npc { get; }
    public ObjectInstance Iterator { get; }
    public DialogContext Context { get; }

    /// <summary>The waiting-on kind. Null = the script finished or hasn't yielded yet.</summary>
    public DialogStepKind? Awaiting { get; internal set; }

    public DialogSession(NpcEntity npc, ObjectInstance iterator, DialogContext context)
    {
        Npc = npc;
        Iterator = iterator;
        Context = context;
    }
}
