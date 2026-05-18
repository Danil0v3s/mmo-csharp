using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task warp(string map, int x, int y)
        => ScriptStub.CallAsync(Cat, "warp", map, x, y);

    public Task savePoint(string map, int x, int y, int rangeX = 0, int rangeY = 0)
        => ScriptStub.CallAsync(Cat, "savePoint", map, x, y, rangeX, rangeY);

    public Task save(string map, int x, int y) => savePoint(map, x, y);

    public object? getSavePoint()
        => ScriptStub.Call<object?>(Cat, "getSavePoint", null);

    public Task pushPc(int direction, int cells)
        => ScriptStub.CallAsync(Cat, "pushPc", direction, cells);

    public Task warpPartner(string map, int x, int y)
        => ScriptStub.CallAsync(Cat, "warpPartner", map, x, y);
}
