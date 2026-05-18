using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class NpcInfo
{
    public int npcTimer(int infoType) => ScriptStub.Call(Cat, "npcTimer", 0, infoType);
    public int getNpcId(int type) => ScriptStub.Call(Cat, "getNpcId", 0, type);
    public string npcInfo(int type) => ScriptStub.Call(Cat, "npcInfo", name, type);
}
