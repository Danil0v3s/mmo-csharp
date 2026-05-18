namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public long getTime(int type) => ScriptStub.Call<long>(Cat, "getTime", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), type);
    public long getTimeTick(int tickType) => ScriptStub.Call<long>(Cat, "getTimeTick", Environment.TickCount64, tickType);
    public string getTimeStr(string format, int maxLength, long? tick = null)
        => ScriptStub.Call(Cat, "getTimeStr", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), format, maxLength, tick);
}
