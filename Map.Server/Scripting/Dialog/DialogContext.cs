using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

/// <summary>
/// The object exposed to TS authors as <c>ctx</c> inside a hook closure.
/// Methods return <see cref="Task"/> / <see cref="Task{T}"/>; ClearScript
/// auto-marshals these to JS Promises, so authors write idiomatic
/// <c>await ctx.mes("...")</c> / <c>const choice = await ctx.select([...])</c>.
///
/// Lowercase method names match the JS contract; ClearScript's host-object
/// binding is case-sensitive when mapping <c>ctx.foo()</c> to a CLR method.
/// </summary>
public sealed class DialogContext
{
    private readonly MapSessionData _session;
    private readonly DialogSession _dialog;

    /// <summary>NPC info exposed to script as <c>ctx.npc</c>.</summary>
    public NpcInfo npc { get; }

    public DialogContext(MapSessionData session, DialogSession dialog, NpcEntity entity)
    {
        _session = session;
        _dialog = dialog;
        npc = new NpcInfo(entity);
    }

    // ---- Non-suspending: send and return. ClearScript marshals Task →
    //      Promise, and an already-completed Task resolves immediately so
    //      the script's `await ctx.mes(...)` just continues.

    public Task mes(string text)
    {
        _session.EnqueuePacket(new ZC_SAY_DIALOG
        {
            NpcId = (uint)_dialog.Npc.Id.Value,
            Message = text ?? string.Empty,
        });
        return Task.CompletedTask;
    }

    // ---- Suspending: send packet, return Task that the resume handler
    //      will complete via the TCS stored on PendingWait.

    public Task next()
    {
        _session.EnqueuePacket(new ZC_WAIT_DIALOG { NpcId = (uint)_dialog.Npc.Id.Value });
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialog.Pending = new PendingWait.Next(tcs);
        return tcs.Task;
    }

    public Task<int> select(object options)
    {
        var menuStr = JoinOptions(options);
        _session.EnqueuePacket(new ZC_MENU_LIST
        {
            NpcId = (uint)_dialog.Npc.Id.Value,
            Menu = menuStr,
        });
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialog.Pending = new PendingWait.Menu(tcs);
        return tcs.Task;
    }

    /// <summary>Alias for <see cref="select"/>; both yield a menu-kind step.</summary>
    public Task<int> menu(object options) => select(options);

    public Task close()
    {
        _session.EnqueuePacket(new ZC_CLOSE_DIALOG { NpcId = (uint)_dialog.Npc.Id.Value });
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialog.Pending = new PendingWait.Close(tcs);
        return tcs.Task;
    }

    private static string JoinOptions(object options)
    {
        // ClearScript hands us a Microsoft.ClearScript.ScriptObject for a JS
        // array. We can iterate via the length + index properties — same
        // shape JsObjectReader uses elsewhere.
        if (options is not Microsoft.ClearScript.ScriptObject arr) return string.Empty;
        var lenProp = arr.GetProperty("length");
        if (lenProp is not int and not double) return string.Empty;
        var len = Convert.ToInt32(lenProp);
        var parts = new string[len];
        for (var i = 0; i < len; i++)
        {
            parts[i] = arr.GetProperty(i)?.ToString() ?? string.Empty;
        }
        return string.Join(":", parts);
    }
}

/// <summary>Read-only NPC info exposed to script as <c>ctx.npc</c>. Lowercase names match the JS contract.</summary>
public sealed class NpcInfo
{
    public string map { get; }
    public int x { get; }
    public int y { get; }
    public int dir { get; }
    public string name { get; }
    public int sprite { get; }

    public NpcInfo(NpcEntity entity)
    {
        // entity.MapId is the runtime hashed id; script-facing string name
        // will land in Phase 3 once MapId→name lookup is wired.
        map = string.Empty;
        x = entity.X;
        y = entity.Y;
        dir = entity.Dir;
        name = entity.Name;
        sprite = entity.SpriteId;
    }
}
