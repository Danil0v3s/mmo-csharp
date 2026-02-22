using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_KEEP_ALIVE)]
public class CharKeepAliveHandler(ILogger<CharKeepAliveHandler> logger) : IPacketHandler<CharSessionData, CH_KEEP_ALIVE>
{
    public Task HandleAsync(CharSessionData session, CH_KEEP_ALIVE packet)
    {
        if (!session.AccountId.HasValue)
        {
            session.AccountId = (int)packet.AccountId;
            logger.LogDebug("Bound account {AccountId} to char session {SessionId} via keepalive", packet.AccountId, session.SessionId);
        }

        return Task.CompletedTask;
    }
}
