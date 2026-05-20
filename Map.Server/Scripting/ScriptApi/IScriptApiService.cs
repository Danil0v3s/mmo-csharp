namespace Map.Server.Scripting.ScriptApi;

/// <summary>
/// Top-level script engine API. Canonical entry points for the
/// scripting surface of rAthena <c>script.cpp</c> (28 422 lines, ~77
/// public functions excluding BUILDINs).
///
/// The C# port replaces rAthena's script engine with TypeScript +
/// Jint (see `.agents/migrations/map/scripting/`). The BUILTINs
/// (`getitem`, `mes`, `npctalk`, etc.) live as TS API functions
/// declared in `scripts/types/api.d.ts`; the engine-level helpers
/// (`run_script`, `script_pop_val`, `set_reg`, `script_reload`)
/// surface here.
/// </summary>
public interface IScriptApiService
{
    /// <summary>rAthena <c>run_script</c> — execute a script by handle.</summary>
    bool RunScript(string scriptId, int callerEntityId);

    /// <summary>rAthena <c>script_reload</c> — reload all NPC scripts.</summary>
    void Reload();

    /// <summary>rAthena <c>script_print_line</c> — engine-side log line.</summary>
    void PrintLine(string text);

    /// <summary>rAthena <c>script_pop_val</c> — pop next argument from the running script's stack.</summary>
    long PopVal(int contextId);

    /// <summary>rAthena <c>script_pop_str</c>.</summary>
    string PopStr(int contextId);

    /// <summary>rAthena <c>script_push_val</c>.</summary>
    void PushVal(int contextId, long value);

    /// <summary>rAthena <c>script_push_str</c>.</summary>
    void PushStr(int contextId, string value);

    /// <summary>rAthena <c>do_init_script</c>.</summary>
    void Init();

    /// <summary>rAthena <c>do_final_script</c>.</summary>
    void Final();
}
