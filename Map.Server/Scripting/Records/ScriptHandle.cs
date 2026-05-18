using Jint.Native;

namespace Map.Server.Scripting.Records;

/// <summary>
/// Opaque handle to a script-side closure. Captured at registration time by
/// <c>RegistrarBindings</c>; consumed at dispatch time by Phase 2's hook
/// invoker (not yet wired). Phase 1 only validates that the underlying
/// <see cref="JsValue"/> is callable.
///
/// The <see cref="Source"/> field carries a human-readable origin for log
/// lines ("scripts/dist/main.js:42:1" or "registerNpc.onClick"). Authors
/// editing scripts get useful traces when a hook misbehaves.
/// </summary>
public sealed record ScriptHandle(JsValue Value, string Source)
{
    /// <summary>True when the underlying JS value is invocable as a function.</summary>
    public bool IsCallable => Value is Jint.Native.Function.Function;
}
