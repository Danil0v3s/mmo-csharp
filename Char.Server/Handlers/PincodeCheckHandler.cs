using Char.Server.Services;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_PINCODE_CHECK)]
public class PincodeCheckHandler(
    ILogger<PincodeCheckHandler> logger,
    CharServerConfiguration configuration,
    ILoginServerIpcService loginServerIpcService
) : IPacketHandler<CharSessionData, CH_PINCODE_CHECK>
{
    public async Task HandleAsync(CharSessionData session, CH_PINCODE_CHECK packet)
    {
        if (!session.AccountId.HasValue || !session.IsAuthenticated || session.AccountId.Value != (int)packet.AccountId)
        {
            logger.LogWarning("Rejecting CH_PINCODE_CHECK from invalid session {SessionId}", session.SessionId);
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        if (!configuration.Pincode.Enabled)
        {
            session.PincodeVerified = true;
            return;
        }

        var expectedPin = PincodeFlowSupport.NormalizePin(session.Pincode);
        var providedPin = PincodeFlowSupport.NormalizePin(packet.PinCode);
        if (!string.IsNullOrEmpty(expectedPin) && expectedPin == providedPin)
        {
            session.PincodeVerified = true;
            session.PincodeAttempts = 0;
            PincodeFlowSupport.SendState(session, PincodeState.PassedOrDisabled);
            return;
        }

        session.PincodeAttempts++;
        PincodeFlowSupport.SendState(session, PincodeState.Incorrect);
        await loginServerIpcService.NotifyPincodeAuthFailAsync(session.AccountId.Value);

        if (session.PincodeAttempts >= Math.Max(configuration.Pincode.MaxTry, 1))
        {
            logger.LogWarning(
                "Disconnecting session {SessionId} for account {AccountId} after too many wrong pincode attempts ({Attempts})",
                session.SessionId,
                session.AccountId.Value,
                session.PincodeAttempts);
            session.Disconnect(DisconnectReason.Kicked);
        }
    }
}
