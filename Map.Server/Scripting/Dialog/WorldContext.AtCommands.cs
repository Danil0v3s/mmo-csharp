namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task atCommand(string command) => ScriptStub.CallAsync(Cat, "atCommand", command);
    public Task charCommand(string command) => ScriptStub.CallAsync(Cat, "charCommand", command);
    public Task useAtCommand(string command) => ScriptStub.CallAsync(Cat, "useAtCommand", command);
    public Task bindAtCommand(string command, string eventTarget, int atLevel = 0, int charLevel = 0)
        => ScriptStub.CallAsync(Cat, "bindAtCommand", command, eventTarget, atLevel, charLevel);
    public Task unbindAtCommand(string command) => ScriptStub.CallAsync(Cat, "unbindAtCommand", command);
}
