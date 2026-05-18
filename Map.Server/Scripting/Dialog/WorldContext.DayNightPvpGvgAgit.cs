namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task day() => ScriptStub.CallAsync(Cat, "day");
    public Task night() => ScriptStub.CallAsync(Cat, "night");
    public bool isDay() => ScriptStub.Call(Cat, "isDay", true);
    public bool isNight() => ScriptStub.Call(Cat, "isNight", false);
    public Task pvpOn(string mapName) => ScriptStub.CallAsync(Cat, "pvpOn", mapName);
    public Task pvpOff(string mapName) => ScriptStub.CallAsync(Cat, "pvpOff", mapName);
    public Task gvgOn(string mapName) => ScriptStub.CallAsync(Cat, "gvgOn", mapName);
    public Task gvgOff(string mapName) => ScriptStub.CallAsync(Cat, "gvgOff", mapName);
    public Task gvgOn3(string mapName) => ScriptStub.CallAsync(Cat, "gvgOn3", mapName);
    public Task gvgOff3(string mapName) => ScriptStub.CallAsync(Cat, "gvgOff3", mapName);
    public Task agitStart(int era = 1) => ScriptStub.CallAsync(Cat, "agitStart", era);
    public Task agitEnd(int era = 1) => ScriptStub.CallAsync(Cat, "agitEnd", era);
    public bool agitCheck(int era = 1) => ScriptStub.Call(Cat, "agitCheck", false, era);
    public Task flagEmblem(int guildId) => ScriptStub.CallAsync(Cat, "flagEmblem", guildId);
    public string castleName(string mapName) => ScriptStub.Call(Cat, "castleName", string.Empty, mapName);
    public int castleData(string mapName, int type) => ScriptStub.Call(Cat, "castleData", 0, mapName, type);
}
