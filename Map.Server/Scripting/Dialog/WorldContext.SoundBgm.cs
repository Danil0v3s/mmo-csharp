namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task soundEffectAll(string filename, int type = 0, string? mapName = null,
        int x0 = 0, int y0 = 0, int x1 = 0, int y1 = 0)
        => ScriptStub.CallAsync(Cat, "soundEffectAll", filename, type, mapName, x0, y0, x1, y1);
    public Task playBgmAll(string filename, string? mapName = null,
        int x0 = 0, int y0 = 0, int x1 = 0, int y1 = 0)
        => ScriptStub.CallAsync(Cat, "playBgmAll", filename, mapName, x0, y0, x1, y1);
}
