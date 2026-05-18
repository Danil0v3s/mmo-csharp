using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task marry(string spouseName) => ScriptStub.CallAsync(Cat, "marry", spouseName);
    public Task divorce() => ScriptStub.CallAsync(Cat, "divorce");
    public Task adopt(string parentName, string babyName)
        => ScriptStub.CallAsync(Cat, "adopt", parentName, babyName);
    public int getPartnerId() => ScriptStub.Call(Cat, "getPartnerId", 0);
    public int getMotherId() => ScriptStub.Call(Cat, "getMotherId", 0);
    public int getFatherId() => ScriptStub.Call(Cat, "getFatherId", 0);
    public int getChildId() => ScriptStub.Call(Cat, "getChildId", 0);
    public bool isPartnerOn() => ScriptStub.Call(Cat, "isPartnerOn", false);
}
