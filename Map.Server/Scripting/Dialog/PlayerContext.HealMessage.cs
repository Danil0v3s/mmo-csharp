using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    /// <summary>Restore HP and optionally SP. Both clamp to their max.</summary>
    public Task heal(int hp, int sp = 0)
    {
        if (hp != 0) this.hp = _entity.Hp + hp;
        if (sp != 0) this.sp = _entity.Sp + sp;
        return Task.CompletedTask;
    }

    /// <summary>Self-only system chat line — handy for debug feedback.</summary>
    public Task message(string text)
    {
        _session.EnqueuePacket(new ZC_NOTIFY_PLAYERCHAT { Message = text ?? string.Empty });
        return Task.CompletedTask;
    }
}
