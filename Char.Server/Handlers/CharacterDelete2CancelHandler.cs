using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_CHAR_DELETE2_CANCEL)]
public class CharacterDelete2CancelHandler(
    ICharacterRepository characterRepository
) : IPacketHandler<CharSessionData, CH_REQ_CHAR_DELETE2_CANCEL>
{
    public async Task HandleAsync(CharSessionData session, CH_REQ_CHAR_DELETE2_CANCEL packet)
    {
        try
        {
            var character = await characterRepository.GetByIdAsync((int)packet.CharId);
            if (character is null || character.AccountId != session.AccountId.Value)
            {
                SendAck(session, packet.CharId, result: 2);
                return;
            }

            // rAthena parity: do not check if it is currently queued; always try to clear delete_date.
            character.DeleteDate = 0;
            await characterRepository.UpdateAsync(character);

            SendAck(session, packet.CharId, result: 1);
        }
        catch
        {
            SendAck(session, packet.CharId, result: 2);
        }
    }

    private static void SendAck(CharSessionData session, uint charId, uint result)
    {
        session.EnqueuePacket(new HC_CHAR_DELETE2_CANCEL_ACK
        {
            CharId = charId,
            Result = result
        });
    }
}
