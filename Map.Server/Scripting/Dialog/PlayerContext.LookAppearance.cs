using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task setLook(int type, int value)
        => ScriptStub.CallAsync(Cat, "setLook", type, value);

    public Task changeLook(int type, int value)
        => ScriptStub.CallAsync(Cat, "changeLook", type, value);

    public int getLook(int type)
        => ScriptStub.Call(Cat, "getLook", 0, type);

    public Task setFont(int font)
        => ScriptStub.CallAsync(Cat, "setFont", font);

    public Task setCart(int type = 1)
        => ScriptStub.CallAsync(Cat, "setCart", type);

    public Task setFalcon(bool flag = true)
        => ScriptStub.CallAsync(Cat, "setFalcon", flag);

    public Task setRiding(bool flag = true)
        => ScriptStub.CallAsync(Cat, "setRiding", flag);

    public Task setDragon(int color = 0)
        => ScriptStub.CallAsync(Cat, "setDragon", color);

    public Task setMadogear(bool flag = true, int type = 0)
        => ScriptStub.CallAsync(Cat, "setMadogear", flag, type);

    public Task setMounting()
        => ScriptStub.CallAsync(Cat, "setMounting");

    public bool checkCart() => ScriptStub.Call(Cat, "checkCart", false);
    public bool checkFalcon() => ScriptStub.Call(Cat, "checkFalcon", false);
    public bool checkRiding() => ScriptStub.Call(Cat, "checkRiding", false);
    public bool checkDragon() => ScriptStub.Call(Cat, "checkDragon", false);
    public bool checkMadogear() => ScriptStub.Call(Cat, "checkMadogear", false);
    public bool checkWug() => ScriptStub.Call(Cat, "checkWug", false);
    public bool isMounting() => ScriptStub.Call(Cat, "isMounting", false);
}
