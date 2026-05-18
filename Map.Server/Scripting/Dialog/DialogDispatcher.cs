using Core.Server.Packets.Out.ZC;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
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
        if (npc.Hooks.OnClick is not { } handle)
        {
            return false;
        }
        if (handle.Value is not Function fn)
        {
            _logger.LogWarning(
                "NPC '{Name}' onClick hook is not a function (got {Type}); ignoring click",
                npc.Name, handle.Value.Type);
            return false;
        }

        // End any in-flight dialog with this player before starting a new one.
        if (session.Dialog != null)
        {
            _logger.LogDebug(
                "Char {CharId} starting NPC '{Name}' while dialog with '{Old}' was open — superseding",
                session.CharacterId, npc.Name, session.Dialog.Npc.Name);
            session.Dialog = null;
        }

        var ctx = new DialogContext(_scriptHost.Engine, npc);

        // Invoke the generator function. Result is the iterator (NOT the
        // body's run result — generators are lazy).
        //
        // IMPORTANT: pass arguments via an explicit JsValue[] array. Calling
        // `fn.Call(thisObj, arg1)` resolves to an extension method overload
        // `JsValueExtensions.Call(value, arg1, arg2)` that treats both args
        // as positional — `thisObj` becomes the script's first parameter and
        // the real ctx wrapper becomes an ignored second arg.
        JsValue iterValue;
        var args = new[] { JsValue.FromObject(_scriptHost.Engine, ctx) };
        try
        {
            iterValue = fn.Call(JsValue.Undefined, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NPC '{Name}' onClick threw on invocation", npc.Name);
            return false;
        }

        if (iterValue is not ObjectInstance iter)
        {
            _logger.LogWarning(
                "NPC '{Name}' onClick did not return an iterator — author must use `function*` / `yield`, " +
                "not `async` / `await`. Got {Type}.",
                npc.Name, iterValue.Type);
            return false;
        }

        var dialog = new DialogSession(npc, iter, ctx);
        session.Dialog = dialog;

        // Drive the iterator until it either suspends on a non-immediate
        // step (menu/next/close) or finishes.
        RunUntilSuspended(session, dialog);
        return true;
    }

    public void ResumeNext(MapSessionData session, uint npcId)
    {
        if (!ValidateResume(session, npcId, DialogStepKind.Next, out var dialog)) return;
        RunUntilSuspended(session, dialog!);
    }

    public void ResumeMenu(MapSessionData session, uint npcId, byte selection)
    {
        if (!ValidateResume(session, npcId, DialogStepKind.Menu, out var dialog)) return;

        // 255 = client pressed Escape. Treat as 0 in lastSelection per
        // rAthena convention; authors can branch on that.
        dialog!.Context.lastSelection = selection == 255 ? 0 : selection;
        RunUntilSuspended(session, dialog);
    }

    public void ResumeClose(MapSessionData session, uint npcId)
    {
        if (!ValidateResume(session, npcId, DialogStepKind.Close, out _)) return;
        // close ends the dialog; whatever the script does after `yield ctx.close()`
        // runs, then the iterator returns {done: true}. Drive it.
        RunUntilSuspended(session, session.Dialog!);
    }

    private bool ValidateResume(
        MapSessionData session,
        uint npcId,
        DialogStepKind expected,
        out DialogSession? dialog)
    {
        dialog = session.Dialog;
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
        if (dialog.Awaiting != expected)
        {
            _logger.LogDebug(
                "Dialog resume expected step {Expected} but client sent {Actual}",
                dialog.Awaiting, expected);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Pull one step at a time from the generator, send the corresponding
    /// packet, and stop on the first step that requires a client response
    /// (Next / Menu / Close). Mes steps don't suspend — they send and
    /// immediately loop to pull the next step.
    /// </summary>
    private void RunUntilSuspended(MapSessionData session, DialogSession dialog)
    {
        var engine = _scriptHost.Engine;
        var iter = dialog.Iterator;
        var nextFn = iter.Get("next");

        while (true)
        {
            JsValue stepResult;
            try
            {
                stepResult = engine.Invoke(nextFn, iter, new JsValue[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "NPC '{Name}' script threw during execution",
                    dialog.Npc.Name);
                EndDialog(session, dialog.Npc.Id.Value);
                return;
            }

            if (stepResult is not ObjectInstance stepObj)
            {
                _logger.LogWarning(
                    "Generator iter.next() returned non-object {Type} — ending dialog",
                    stepResult.Type);
                EndDialog(session, dialog.Npc.Id.Value);
                return;
            }

            if (stepObj.Get("done").AsBoolean())
            {
                // Script ran to completion without yielding a close step.
                // Send a close packet so the client unblocks; the dialog
                // is over.
                EndDialog(session, dialog.Npc.Id.Value);
                return;
            }

            var stepValue = stepObj.Get("value");
            if (stepValue is not ObjectInstance step)
            {
                _logger.LogWarning(
                    "NPC '{Name}' yielded a non-object value (got {Type}); ending dialog. " +
                    "Authors must yield the result of a ctx method, e.g. `yield ctx.mes(\"hi\")`.",
                    dialog.Npc.Name, stepValue.Type);
                EndDialog(session, dialog.Npc.Id.Value);
                return;
            }

            var kindStr = step.Get("kind");
            if (!kindStr.IsString())
            {
                _logger.LogWarning(
                    "Yielded step has no string `kind` field; ending dialog");
                EndDialog(session, dialog.Npc.Id.Value);
                return;
            }

            switch (kindStr.AsString())
            {
                case "mes":
                    var text = step.Get("text").IsString() ? step.Get("text").AsString() : string.Empty;
                    session.EnqueuePacket(new ZC_SAY_DIALOG
                    {
                        NpcId = (uint)dialog.Npc.Id.Value,
                        Message = text,
                    });
                    // mes does NOT suspend; loop to pull the next step.
                    continue;

                case "next":
                    session.EnqueuePacket(new ZC_WAIT_DIALOG
                    {
                        NpcId = (uint)dialog.Npc.Id.Value,
                    });
                    dialog.Awaiting = DialogStepKind.Next;
                    return;

                case "menu":
                    var optionsJs = step.Get("options");
                    var menuStr = JoinMenuOptions(optionsJs);
                    session.EnqueuePacket(new ZC_MENU_LIST
                    {
                        NpcId = (uint)dialog.Npc.Id.Value,
                        Menu = menuStr,
                    });
                    dialog.Awaiting = DialogStepKind.Menu;
                    return;

                case "close":
                    session.EnqueuePacket(new ZC_CLOSE_DIALOG
                    {
                        NpcId = (uint)dialog.Npc.Id.Value,
                    });
                    dialog.Awaiting = DialogStepKind.Close;
                    return;

                default:
                    _logger.LogWarning(
                        "Yielded step has unknown kind '{Kind}'; ending dialog",
                        kindStr.AsString());
                    EndDialog(session, dialog.Npc.Id.Value);
                    return;
            }
        }
    }

    private static string JoinMenuOptions(JsValue options)
    {
        if (options is not ObjectInstance arr || !arr.IsArray()) return string.Empty;
        var len = (int)arr.Get("length").AsNumber();
        var parts = new string[len];
        for (var i = 0; i < len; i++)
        {
            var v = arr.Get(i.ToString());
            parts[i] = v.IsString() ? v.AsString() : v.ToString();
        }
        return string.Join(":", parts);
    }

    private void EndDialog(MapSessionData session, int npcId)
    {
        session.EnqueuePacket(new ZC_CLOSE_DIALOG { NpcId = (uint)npcId });
        session.Dialog = null;
    }
}
