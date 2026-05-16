using Char.Server.Services;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_PINCODE_WINDOW)]
public class PincodeWindowHandler(
    ILogger<PincodeWindowHandler> logger,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_REQ_PINCODE_WINDOW>
{
    public async Task HandleAsync(CharSessionData session, CH_REQ_PINCODE_WINDOW packet)
    {
        if (!session.AccountId.HasValue || !session.IsAuthenticated || session.AccountId.Value != (int)packet.AccountId)
        {
            logger.LogWarning("Rejecting CH_REQ_PINCODE_WINDOW from invalid session {SessionId}", session.SessionId);
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        if (!configuration.Pincode.Enabled)
        {
            return;
        }

        if (session.PincodeVerified)
        {
            return;
        }

        PincodeFlowSupport.SendState(session, PincodeFlowSupport.ComputeWindowState(session));
    }
}
