using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task dispBottom(string text, int color = -1)
        => ScriptStub.CallAsync(Cat, "dispBottom", text, color);

    public Task showScript(string text, int flag = 0)
        => ScriptStub.CallAsync(Cat, "showScript", text, flag);

    public Task cutin(string filename, int position)
        => ScriptStub.CallAsync(Cat, "cutin", filename, position);

    public Task emotion(int emoNum, int target = 0)
        => ScriptStub.CallAsync(Cat, "emotion", emoNum, target);

    public Task miscEffect(int effectNum)
        => ScriptStub.CallAsync(Cat, "miscEffect", effectNum);

    public Task soundEffect(string filename, int type = 0)
        => ScriptStub.CallAsync(Cat, "soundEffect", filename, type);

    public Task playBgm(string filename)
        => ScriptStub.CallAsync(Cat, "playBgm", filename);

    public Task viewpoint(int action, int x, int y, int point, int color)
        => ScriptStub.CallAsync(Cat, "viewpoint", action, x, y, point, color);

    public Task showDigit(int value, int type = 0)
        => ScriptStub.CallAsync(Cat, "showDigit", value, type);

    public Task hatEffect(int hatEffectId, bool state)
        => ScriptStub.CallAsync(Cat, "hatEffect", hatEffectId, state);
}
