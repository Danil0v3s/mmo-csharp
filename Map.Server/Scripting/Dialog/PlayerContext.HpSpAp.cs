using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int hp
    {
        get => _entity.Hp;
        set
        {
            _entity.Hp = Math.Clamp(value, 0, _entity.MaxHp);
            MarkDirty(SpId.SP_HP, _entity.Hp);
        }
    }
    public int maxHp => _entity.MaxHp;
    public int sp
    {
        get => _entity.Sp;
        set
        {
            _entity.Sp = Math.Clamp(value, 0, _entity.MaxSp);
            MarkDirty(SpId.SP_SP, _entity.Sp);
        }
    }
    public int maxSp => _entity.MaxSp;
    public int ap => ScriptStub.Call(Cat, "ap", 0);
    public int maxAp => ScriptStub.Call(Cat, "maxAp", 0);
}
