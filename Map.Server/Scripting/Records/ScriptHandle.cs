using Microsoft.ClearScript;

namespace Map.Server.Scripting.Records;

/// <summary>
/// Opaque handle to a script-side closure. Captured at registration time by
/// <c>RegistrarBindings</c>; consumed at dispatch time by the
/// <c>DialogDispatcher</c>. We invoke the handle by calling
/// <see cref="ScriptObject.Invoke"/> with the desired arguments.
///
/// The <see cref="Source"/> field carries a human-readable origin for log
/// lines ("registerNpc('Kafra').onClick"). Authors editing scripts get
/// useful traces when a hook misbehaves.
/// </summary>
public sealed record ScriptHandle(ScriptObject Value, string Source);
