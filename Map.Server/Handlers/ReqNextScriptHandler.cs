using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Scripting.Dialog;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// Client clicked the "Next" button in an open NPC dialog. Resume the
/// player's dialog generator. Mirrors rAthena <c>clif_parse_NextScript</c>.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_NEXT_SCRIPT)]
public class ReqNextScriptHandler(IDialogDispatcher dispatcher)
    : IPacketHandler<MapSessionData, CZ_REQ_NEXT_SCRIPT>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_NEXT_SCRIPT packet)
    {
        dispatcher.ResumeNext(session, packet.NpcId);
        return Task.CompletedTask;
    }
}
