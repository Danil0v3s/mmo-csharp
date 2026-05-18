using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    private void MarkDirty(ushort spId, int newValue)
    {
        _dirty[spId] = newValue;
    }

    /// <summary>
    /// Emit ZC_PAR_CHANGE / ZC_LONGPAR_CHANGE for every stat the script
    /// touched since the last flush, then clear the dirty set. Called by
    /// <see cref="DialogContext"/> on every suspending step so the wire
    /// order is: pending stat updates → dialog packet.
    /// </summary>
    internal void Flush()
    {
        if (_dirty.Count == 0) return;
        foreach (var (spId, value) in _dirty)
        {
            if (LongParStats.Contains(spId))
            {
                _session.EnqueuePacket(new ZC_LONGPAR_CHANGE { VarId = spId, Value = value });
            }
            else
            {
                _session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = spId, Value = value });
            }
        }
        _dirty.Clear();
    }
}
