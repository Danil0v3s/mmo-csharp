using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public bool permissionCheck(string permission)
        => ScriptStub.Call(Cat, "permissionCheck", false, permission);
    public Task permissionAdd(string permission)
        => ScriptStub.CallAsync(Cat, "permissionAdd", permission);
    public Task permissionRemove(string permission)
        => ScriptStub.CallAsync(Cat, "permissionRemove", permission);
    public bool guildHasPermission(string permission)
        => ScriptStub.Call(Cat, "guildHasPermission", false, permission);
}
