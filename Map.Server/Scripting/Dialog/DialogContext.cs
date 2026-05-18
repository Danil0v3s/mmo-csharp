using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
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
public sealed partial class DialogContext
{
    private const string Cat = "ctx";

    private readonly MapSessionData _session;
    private readonly DialogSession _dialog;

    /// <summary>NPC info exposed to script as <c>ctx.npc</c>.</summary>
    public NpcInfo npc { get; }

    /// <summary>
    /// Player surface exposed to script as <c>ctx.player</c>. Always non-null
    /// while the dialog is active (a dialog is only initiated by a player
    /// click). Null projection is OK in the type signature for symmetry with
    /// non-click hook flavours (onInit/onTimer/onClock) — Phase 5 will use
    /// the same DialogContext shape there with player = null.
    /// </summary>
    public PlayerContext? player { get; }

    /// <summary>Map / server-wide ops (announce, spawn, mapflags, etc.).</summary>
    public WorldContext world { get; }
    /// <summary>Party ops (create, leader, members).</summary>
    public PartyContext party { get; }
    /// <summary>Guild ops (info, master, members).</summary>
    public GuildContext guild { get; }
    /// <summary>Instance ops (create, enter, vars).</summary>
    public InstanceContext instance { get; }
    /// <summary>Battleground ops (create, join, monsters).</summary>
    public BattlegroundContext bg { get; }
    /// <summary>Channel ops (create, chat, ban).</summary>
    public ChannelContext channel { get; }

    public DialogContext(
        MapSessionData session,
        DialogSession dialog,
        NpcEntity entity,
        PlayerEntity? playerEntity,
        Map.Server.Inventory.IInventoryService inventory)
    {
        _session = session;
        _dialog = dialog;
        npc = new NpcInfo(entity);
        player = playerEntity != null ? new PlayerContext(session, playerEntity, inventory) : null;
        world = new WorldContext();
        party = new PartyContext();
        guild = new GuildContext();
        instance = new InstanceContext();
        bg = new BattlegroundContext();
        channel = new ChannelContext();
    }

    // ---- Non-suspending: send and return. ClearScript marshals Task →
    //      Promise, and an already-completed Task resolves immediately so
    //      the script's `await ctx.mes(...)` just continues.
    //
    //      Every suspending / sending method flushes pending player-stat
    //      updates first so their packets land before the dialog packet
    //      (correct order from the client's POV: "you have 100z" only
    //       after the zeny counter visually updates).

    public Task mes(string text)
    {
        FlushPlayerDirty();
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
        FlushPlayerDirty();
        _session.EnqueuePacket(new ZC_WAIT_DIALOG { NpcId = (uint)_dialog.Npc.Id.Value });
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialog.Pending = new PendingWait.Next(tcs);
        return tcs.Task;
    }

    public Task<int> select(object options)
    {
        FlushPlayerDirty();
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
        FlushPlayerDirty();
        _session.EnqueuePacket(new ZC_CLOSE_DIALOG { NpcId = (uint)_dialog.Npc.Id.Value });
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialog.Pending = new PendingWait.Close(tcs);
        return tcs.Task;
    }

    /// <summary>
    /// Drain any pending player-stat mutations into outgoing ZC_PAR /
    /// ZC_LONGPAR packets. No-op when nothing is dirty. Used by every
    /// sending / suspending host method so client packets land in
    /// deterministic order with dialog steps.
    /// </summary>
    private void FlushPlayerDirty() => player?.Flush();

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

/// <summary>
/// Read facts and display / movement / shop ops on the NPC the dialog is
/// attached to. Exposed to script as <c>ctx.npc</c>. Lowercase member
/// names match the JS contract.
/// </summary>
public sealed partial class NpcInfo
{
    private const string Cat = "npc";

    public string map { get; }
    public int x { get; }
    public int y { get; }
    public int dir { get; }
    public string name { get; }
    public int sprite { get; }

    /// <summary>NPC-local variables. Memory-only; reset on script reload.</summary>
    public PropertyBag vars { get; }

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
        vars = new PropertyBag();
    }
}
