using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed class DialogDispatcher : IDialogDispatcher
{
    private readonly ScriptHost _scriptHost;
    private readonly ILogger<DialogDispatcher> _logger;

    public DialogDispatcher(ScriptHost scriptHost, ILogger<DialogDispatcher> logger)
    {
        _scriptHost = scriptHost;
        _logger = logger;
    }

    public bool StartOnClick(MapSessionData session, NpcEntity npc)
    {
        if (npc.Hooks.OnClick is not { } handle) return false;

        // End any in-flight dialog with this player before starting a new one.
        if (session.Dialog != null)
        {
            _logger.LogDebug(
                "Char {CharId} starting NPC '{Name}' while dialog with '{Old}' was open — superseding",
                session.CharacterId, npc.Name, session.Dialog.Npc.Name);
            session.Dialog = null;
        }

        var dialog = new DialogSession(npc);
        var ctx = new DialogContext(session, dialog, npc);
        dialog.Context = ctx;
        session.Dialog = dialog;

        try
        {
            // Invoke the async function. With EnableTaskPromiseConversion, the
            // returned JS Promise comes back as a .NET Task; we don't await it
            // (it completes when the whole script returns, after `await ctx.close()`).
            // The script's per-step awaits are driven by the TCS objects we
            // hand it via ctx.next()/select()/close().
            var result = handle.Value.Invoke(asConstructor: false, ctx);

            // If the script returned a Task (because it's async), attach a
            // continuation so any unhandled exception inside the script
            // surfaces to our logs and ends the dialog cleanly.
            if (result is Task t)
            {
                t.ContinueWith(completed =>
                {
                    if (completed.IsFaulted)
                    {
                        _logger.LogError(completed.Exception,
                            "NPC '{Name}' onClick threw", npc.Name);
                        TryEnd(session, dialog);
                    }
                }, TaskScheduler.Default);
            }
        }
        catch (ScriptEngineException ex)
        {
            _logger.LogError(ex,
                "NPC '{Name}' onClick threw at invocation: {Details}",
                npc.Name, ex.ErrorDetails);
            TryEnd(session, dialog);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NPC '{Name}' onClick threw at invocation", npc.Name);
            TryEnd(session, dialog);
            return false;
        }

        return true;
    }

    public void ResumeNext(MapSessionData session, uint npcId)
    {
        if (!ValidateResume<PendingWait.Next>(session, npcId, out var wait)) return;
        session.Dialog!.Pending = null;
        wait!.Tcs.TrySetResult();
    }

    public void ResumeMenu(MapSessionData session, uint npcId, byte selection)
    {
        if (!ValidateResume<PendingWait.Menu>(session, npcId, out var wait)) return;
        session.Dialog!.Pending = null;
        // rAthena convention: 255 = Escape → 0. Otherwise 1-based.
        var choice = selection == 255 ? 0 : selection;
        wait!.Tcs.TrySetResult(choice);
    }

    public void ResumeClose(MapSessionData session, uint npcId)
    {
        if (!ValidateResume<PendingWait.Close>(session, npcId, out var wait)) return;
        session.Dialog!.Pending = null;
        wait!.Tcs.TrySetResult();
        // Whatever code follows `await ctx.close()` runs to completion. When
        // the script's outer Promise settles, the dialog is over.
        session.Dialog = null;
    }

    private bool ValidateResume<T>(MapSessionData session, uint npcId, out T? wait)
        where T : PendingWait
    {
        wait = null;
        var dialog = session.Dialog;
        if (dialog == null)
        {
            _logger.LogDebug(
                "Client {CharId} sent dialog resume for NPC {NpcId} but no dialog is open",
                session.CharacterId, npcId);
            return false;
        }
        if (dialog.Npc.Id.Value != npcId)
        {
            _logger.LogDebug(
                "Dialog resume targets wrong NPC: expected {Expected}, got {Got}",
                dialog.Npc.Id.Value, npcId);
            return false;
        }
        if (dialog.Pending is not T match)
        {
            _logger.LogDebug(
                "Dialog resume expected {Expected} but client sent {Actual}",
                typeof(T).Name, dialog.Pending?.GetType().Name ?? "(nothing)");
            return false;
        }
        wait = match;
        return true;
    }

    private static void TryEnd(MapSessionData session, DialogSession dialog)
    {
        session.EnqueuePacket(new ZC_CLOSE_DIALOG { NpcId = (uint)dialog.Npc.Id.Value });
        session.Dialog = null;
    }
}
