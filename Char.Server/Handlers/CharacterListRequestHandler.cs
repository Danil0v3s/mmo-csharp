using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;

namespace Char.Server.Handlers;

/// <summary>
/// Handles character list request packets.
/// Note: This is a placeholder - actual CH_CHARLIST_REQ packet class needs to be implemented.
/// </summary>
[PacketHandler(PacketHeader.CH_CHARLIST_REQ)]
public class CharacterListRequestHandler : IPacketHandler<CZ_HEARTBEAT>
{
    private readonly ILogger<CharacterListRequestHandler> _logger;

    public CharacterListRequestHandler(ILogger<CharacterListRequestHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ClientSession session, CZ_HEARTBEAT packet)
    {
        _logger.LogInformation("Character list request from session {SessionId}", session.SessionId);

        // TODO: Query characters from database
        // For now, return mock data using HC_CHARACTER_LIST
        var responsePacket = new HC_CHARACTER_LIST
        {
            Characters = new[]
            {
                new CharacterInfo
                {
                    CharId = 1001,
                    Name = "Warrior123",
                    Exp = 50000,
                    Zeny = 10000,
                    JobLevel = 50
                },
                new CharacterInfo
                {
                    CharId = 1002,
                    Name = "Mage456",
                    Exp = 45000,
                    Zeny = 8000,
                    JobLevel = 45
                }
            }
        };

        session.EnqueuePacket(responsePacket);

        await Task.CompletedTask;
    }
}

