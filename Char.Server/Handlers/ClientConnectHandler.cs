using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_TO_CONNECT)]
public class ClientConnectHandler(
    ILogger<ClientConnectHandler> logger,
    CharServerImpl charServer
) : IPacketHandler<CharSessionData, CH_REQ_TO_CONNECT>
{
    public async Task HandleAsync(CharSessionData session, CH_REQ_TO_CONNECT packet)
    {
        var authResponse = await charServer.AuthenticateAccountWithLoginServerAsync(
            (int)packet.AccountId,
            (int)packet.LoginId1,
            (int)packet.LoginId2,
            packet.Sex,
            requestId: (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % int.MaxValue));

        if (authResponse?.Success != true)
        {
            logger.LogInformation(
                "Char auth rejected for account {AccountId} (session {SessionId})",
                packet.AccountId,
                session.SessionId);

            session.EnqueuePacket(new HC_REFUSE_ENTER
            {
                ErrorCode = 0
            });
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        session.AccountId = (int)packet.AccountId;

        logger.LogInformation(
            "Char auth accepted for account {AccountId} (session {SessionId})",
            packet.AccountId,
            session.SessionId);
    }
}
