using Jint;
using Jint.Native;
using Jint.Native.Object;
using Map.Server.Entities;
// JsObject lives in Jint.Native; ObjectInstance in Jint.Native.Object.
using JsObject = Jint.Native.JsObject;

namespace Map.Server.Scripting.Dialog;

/// <summary>
/// The object exposed to TS authors as <c>ctx</c> inside a hook closure. JS
/// authors call <c>yield ctx.mes(...)</c> / <c>yield ctx.select(...)</c> etc.;
/// each call returns a tagged descriptor that the dispatcher unwraps to
/// decide what packet to send.
///
/// Mutable fields like <see cref="LastSelection"/> exist because Jint 4.0.3
/// drops the yielded value when <c>yield</c> is the RHS of an assignment
/// expression. To read a client response, authors yield the menu/input
/// descriptor first, then read the stashed result in a separate statement:
/// <code>
///   yield ctx.select(["A", "B"]);
///   const choice = ctx.lastSelection;  // 1-based
/// </code>
///
/// Lowercase method names match the JS contract; Jint's ObjectWrapper is
/// case-sensitive when mapping <c>ctx.foo()</c> to a CLR method.
/// </summary>
public sealed class DialogContext
{
    private readonly Engine _engine;

    /// <summary>The NPC this dialog is bound to. Available to script as <c>ctx.npc</c>.</summary>
    public NpcInfo npc { get; }

    /// <summary>
    /// 1-based selection from the last <c>yield ctx.select(...)</c>. 0 if no menu
    /// has been answered yet. Author reads this AFTER the yield (never as RHS
    /// of an assignment that contains the yield itself — see class summary).
    /// </summary>
    public int lastSelection { get; internal set; }

    public DialogContext(Engine engine, NpcEntity entity)
    {
        _engine = engine;
        npc = new NpcInfo(entity);
    }

    // ---- methods the script yields ----

    public ObjectInstance mes(string text) => Step(DialogStepKind.Mes, text);
    public ObjectInstance next() => Step(DialogStepKind.Next);
    public ObjectInstance select(JsValue options) => Step(DialogStepKind.Menu, options: options);
    public ObjectInstance menu(JsValue options) => Step(DialogStepKind.Menu, options: options);
    public ObjectInstance close() => Step(DialogStepKind.Close);

    private ObjectInstance Step(DialogStepKind kind, string? text = null, JsValue? options = null)
    {
        var obj = new JsObject(_engine);
        obj.FastSetDataProperty("kind", kind switch
        {
            DialogStepKind.Mes => "mes",
            DialogStepKind.Next => "next",
            DialogStepKind.Menu => "menu",
            DialogStepKind.Close => "close",
            _ => throw new ArgumentOutOfRangeException(),
        });
        if (text != null) obj.FastSetDataProperty("text", text);
        if (options != null) obj.FastSetDataProperty("options", options);
        return obj;
    }
}

/// <summary>
/// Read-only NPC info exposed to script as <c>ctx.npc</c>. Lowercase method
/// names match the JS contract.
/// </summary>
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
        // entity.MapId is the runtime numeric id — for the script-facing name
        // we'd want the map_index string. The NpcRegistration carries this
        // through; for now expose what we have (Phase 3 will swap once
        // MapId→name lookup lands).
        map = string.Empty;
        x = entity.X;
        y = entity.Y;
        dir = entity.Dir;
        name = entity.Name;
        sprite = entity.SpriteId;
    }
}
