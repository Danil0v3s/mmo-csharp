using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_KEEP_ALIVE)]
public class CharKeepAliveHandler(ILogger<CharKeepAliveHandler> logger) : IPacketHandler<CharSessionData, CH_KEEP_ALIVE>
{
    public Task HandleAsync(CharSessionData session, CH_KEEP_ALIVE packet)
    {
        if (!session.AccountId.HasValue || session.AccountId.Value != (int)packet.AccountId)
        {
            logger.LogWarning(
                "Rejecting CH_KEEP_ALIVE with mismatched/unbound account id {PacketAccountId} for session {SessionId} (session account {SessionAccountId})",
                packet.AccountId,
                session.SessionId,
                session.AccountId);
            session.Disconnect(DisconnectReason.Kicked);
        }

        return Task.CompletedTask;
    }
}
