using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Network;
using Char.Server.Services;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_CHARLIST)]
public class CharacterListHandler(
    ICharacterListFlowService characterListFlowService
) : IPacketHandler<CharSessionData, CH_REQ_CHARLIST>
{
    public async Task HandleAsync(CharSessionData session, CH_REQ_CHARLIST packet)
    {
        await characterListFlowService.TrySendCharacterPageAsync(session);
    }
}
