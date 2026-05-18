using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int zeny
    {
        get => (int)(_session.CharacterData?.Zeny ?? 0);
        set
        {
            if (_session.CharacterData is null) return;
            var clamped = Math.Max(0, value);
            _session.CharacterData.Zeny = (uint)clamped;
            MarkDirty(SpId.SP_ZENY, clamped);
        }
    }
}
