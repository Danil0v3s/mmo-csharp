using Microsoft.Extensions.Logging;

namespace Map.Server.Scripting.ScriptApi;

/// <summary>
/// Default <see cref="IScriptApiService"/>. Stubbed — the live
/// script engine lives in <c>Map.Server.Scripting.ScriptHost</c>.
/// This service is the rAthena-name shim so a port reading
/// <c>script.cpp</c> can find a single named call.
/// </summary>
public sealed class ScriptApiService : IScriptApiService
{
    private readonly ILogger<ScriptApiService> _logger;
    public ScriptApiService(ILogger<ScriptApiService> logger) => _logger = logger;

    public bool RunScript(string scriptId, int callerEntityId) => false;
    public void Reload() { /* ScriptHost owns reload */ }
    public void PrintLine(string text) => _logger.LogInformation("[script] {Text}", text);
    public long PopVal(int contextId) => 0;
    public string PopStr(int contextId) => "";
    public void PushVal(int contextId, long value) { }
    public void PushStr(int contextId, string value) { }
    public void Init() { }
    public void Final() { }
}
