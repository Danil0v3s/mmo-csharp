using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_CHAR_DELETE2)]
public class CharacterDelete2RequestHandler(
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_REQ_CHAR_DELETE2>
{
    public async Task HandleAsync(CharSessionData session, CH_REQ_CHAR_DELETE2 packet)
    {
        try
        {
            var character = await characterRepository.GetByIdAsync((int)packet.CharId);
            if (character is null || character.AccountId != session.AccountId.Value)
            {
                SendAck(session, packet.CharId, result: 3, deleteDate: 0);
                return;
            }

            if (character.DeleteDate != 0)
            {
                SendAck(session, packet.CharId, result: 0, deleteDate: 0);
                return;
            }

            if ((configuration.Char.CharDeleteRestriction & 0x02) != 0 && character.GuildId != 0)
            {
                SendAck(session, packet.CharId, result: 4, deleteDate: 0);
                return;
            }

            if ((configuration.Char.CharDeleteRestriction & 0x01) != 0 && character.PartyId != 0)
            {
                SendAck(session, packet.CharId, result: 5, deleteDate: 0);
                return;
            }

            var deleteDate = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + configuration.Char.CharDeleteDelay);
            character.DeleteDate = deleteDate;

            await characterRepository.UpdateAsync(character);

            SendAck(session, packet.CharId, result: 1, deleteDate: deleteDate);
        }
        catch
        {
            SendAck(session, packet.CharId, result: 3, deleteDate: 0);
        }
    }

    private static void SendAck(CharSessionData session, uint charId, uint result, uint deleteDate)
    {
        session.EnqueuePacket(new HC_CHAR_DELETE2_ACK
        {
            CharId = charId,
            Result = result,
            DeleteDate = deleteDate
        });
    }
}
