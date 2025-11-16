using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_SELECT_CHAR)]
public class CharacterSelectHandler : IPacketHandler<CharSessionData, CZ_HEARTBEAT>
{
    private readonly ILogger<CharacterSelectHandler> _logger;

    public CharacterSelectHandler(ILogger<CharacterSelectHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(CharSessionData session, CZ_HEARTBEAT packet)
    {
        _logger.LogInformation("Character list request from session {SessionId}", session.SessionId);

        // TODO: implement correct packets
        var responsePacket = new HC_SEND_MAP_DATA();

        session.EnqueuePacket(responsePacket);

        await Task.CompletedTask;
    }
}