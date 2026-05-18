using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class NpcInfo
{
    public Task createWaitingRoom(string roomName, int limit, object? opts = null)
        => ScriptStub.CallAsync(Cat, "createWaitingRoom", roomName, limit, opts);
    public Task removeWaitingRoom() => ScriptStub.CallAsync(Cat, "removeWaitingRoom");
    public Task enableWaitingRoom() => ScriptStub.CallAsync(Cat, "enableWaitingRoom");
    public Task disableWaitingRoom()=> ScriptStub.CallAsync(Cat, "disableWaitingRoom");
    public int getWaitingRoomState(int infoType) => ScriptStub.Call(Cat, "getWaitingRoomState", 0, infoType);
    public Task warpWaitingPc(string map, int x, int y, int? count = null)
        => ScriptStub.CallAsync(Cat, "warpWaitingPc", map, x, y, count);
    public Task kickWaitingRoomUser(string charName)
        => ScriptStub.CallAsync(Cat, "kickWaitingRoomUser", charName);
    public Task kickAllWaitingRoom() => ScriptStub.CallAsync(Cat, "kickAllWaitingRoom");
    public int getWaitingRoomUsers() => ScriptStub.Call(Cat, "getWaitingRoomUsers", 0);
}
