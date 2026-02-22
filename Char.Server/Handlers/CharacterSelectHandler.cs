using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_SELECT_CHAR)]
public class CharacterSelectHandler : IPacketHandler<CharSessionData, CH_SELECT_CHAR>
{
    private readonly ILogger<CharacterSelectHandler> _logger;

    public CharacterSelectHandler(ILogger<CharacterSelectHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(CharSessionData session, CH_SELECT_CHAR packet)
    {
        if (!session.AccountId.HasValue)
        {
            _logger.LogWarning("Rejecting CH_SELECT_CHAR from unauthenticated session {SessionId}", session.SessionId);
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        _logger.LogInformation(
            "Character select request from account {AccountId}, slot {Slot}, session {SessionId}",
            session.AccountId.Value,
            packet.Slot,
            session.SessionId);

        // TODO: implement correct packets
        var responsePacket = new HC_SEND_MAP_DATA();

        session.EnqueuePacket(responsePacket);

        await Task.CompletedTask;
    }
}
