using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Scripting.Dialog;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// Client clicked the "Close" button in an open dialog. Finalises the
/// dialog session: any script code following <c>yield ctx.close()</c> runs
/// to completion, then the dialog ends.
/// </summary>
[PacketHandler(PacketHeader.CZ_CLOSE_DIALOG)]
public class CloseDialogHandler(IDialogDispatcher dispatcher)
    : IPacketHandler<MapSessionData, CZ_CLOSE_DIALOG>
{
    public Task HandleAsync(MapSessionData session, CZ_CLOSE_DIALOG packet)
    {
        dispatcher.ResumeClose(session, packet.NpcId);
        return Task.CompletedTask;
    }
}
