using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Char.Server;

/// <summary>
/// Handles all incoming packets for the Char Server.
/// </summary>
public class CharPacketHandler : PacketHandler
{
    public CharPacketHandler(ILogger logger) : base(logger)
    {
    }

    public override async Task HandlePacketAsync(ClientSession session, IncomingPacket packet)
    {
        switch (packet)
        {
            // TODO: Implement CH_CHARLIST_REQ, CH_MAKE_CHAR, CH_SELECT_CHAR packet classes
            // Example structure:
            // case CH_CHARLIST_REQ charListReq:
            //     await HandleCharacterListRequestAsync(session, charListReq);
            //     break;

            default:
                Logger.LogWarning("Unhandled packet type: {PacketType} (Header: 0x{Header:X4}) from session {SessionId}",
                    packet.GetType().Name, (short)packet.Header, session.SessionId);
                break;
        }

        await Task.CompletedTask;
    }

    private async Task HandleCharacterListRequestAsync(ClientSession session)
    {
        Logger.LogInformation("Character list request from session {SessionId}", session.SessionId);

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

        SendPacket(session, responsePacket);

        await Task.CompletedTask;
    }

    private async Task HandleCharacterCreateAsync(ClientSession session, string characterName, byte classId)
    {
        Logger.LogInformation("Character create request: {CharName}, Class: {ClassId}",
            characterName, classId);

        // TODO: Create character in database
        // TODO: Send HC_ACCEPT_MAKECHAR or HC_REFUSE_MAKECHAR

        await Task.CompletedTask;
    }

    private async Task HandleCharacterSelectAsync(ClientSession session, int characterId)
    {
        Logger.LogInformation("Character select request: {CharId}", characterId);

        // TODO: Load character data and send HC_NOTIFY_ZONESVR

        await Task.CompletedTask;
    }
}

