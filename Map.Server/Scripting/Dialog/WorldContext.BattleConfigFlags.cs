namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task setBattleFlag(string flagName, int value, bool reload = false)
        => ScriptStub.CallAsync(Cat, "setBattleFlag", flagName, value, reload);
    public int getBattleFlag(string flagName)
        => ScriptStub.Call(Cat, "getBattleFlag", 0, flagName);
}
