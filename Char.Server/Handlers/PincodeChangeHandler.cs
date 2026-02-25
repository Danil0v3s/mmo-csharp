using Char.Server.Services;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_PINCODE_CHANGE)]
public class PincodeChangeHandler(
    ILogger<PincodeChangeHandler> logger,
    CharServerConfiguration configuration,
    ILoginServerIpcService loginServerIpcService
) : IPacketHandler<CharSessionData, CH_PINCODE_CHANGE>
{
    public async Task HandleAsync(CharSessionData session, CH_PINCODE_CHANGE packet)
    {
        if (!session.AccountId.HasValue || !session.IsAuthenticated || session.AccountId.Value != (int)packet.AccountId)
        {
            logger.LogWarning("Rejecting CH_PINCODE_CHANGE from invalid session {SessionId}", session.SessionId);
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        if (!configuration.Pincode.Enabled)
        {
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        var oldPin = PincodeFlowSupport.NormalizePin(packet.OldPin);
        var newPin = PincodeFlowSupport.NormalizePin(packet.NewPin);
        if (PincodeFlowSupport.NormalizePin(session.Pincode) != oldPin)
        {
            PincodeFlowSupport.SendState(session, PincodeState.Incorrect);
            return;
        }

        if (!PincodeFlowSupport.IsAllowedPin(newPin, configuration.Pincode))
        {
            PincodeFlowSupport.SendState(session, PincodeState.Illegal);
            return;
        }

        var update = await loginServerIpcService.UpdateAccountPincodeAsync(session.AccountId.Value, newPin);
        if (update?.Success != true)
        {
            logger.LogWarning(
                "Failed to change pincode for account {AccountId} (session {SessionId})",
                session.AccountId.Value,
                session.SessionId);
            PincodeFlowSupport.SendState(session, PincodeState.Incorrect);
            return;
        }

        session.Pincode = newPin;
        session.PincodeVerified = true;
        session.PincodeAttempts = 0;
        session.PincodeChangeUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PincodeFlowSupport.SendState(session, PincodeState.PassedOrDisabled);
    }
}
