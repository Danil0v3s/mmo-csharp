namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task announce(string message, object? opts = null)
        => ScriptStub.CallAsync(Cat, "announce", message, opts);
    public Task mapAnnounce(string mapName, string message, object? opts = null)
        => ScriptStub.CallAsync(Cat, "mapAnnounce", mapName, message, opts);
    public Task areaAnnounce(string mapName, int x1, int y1, int x2, int y2, string message, object? opts = null)
        => ScriptStub.CallAsync(Cat, "areaAnnounce", mapName, x1, y1, x2, y2, message, opts);
    public Task globalMessage(string message, string? fromNpcName = null)
        => ScriptStub.CallAsync(Cat, "globalMessage", message, fromNpcName);
    public Task debugMessage(string message)
        => ScriptStub.CallAsync(Cat, "debugMessage", message);
    public Task errorMessage(string message)
        => ScriptStub.CallAsync(Cat, "errorMessage", message);
    public Task logMessage(string message)
        => ScriptStub.CallAsync(Cat, "logMessage", message);
}
