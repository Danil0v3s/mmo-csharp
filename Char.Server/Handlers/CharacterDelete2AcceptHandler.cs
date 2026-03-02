using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_CHAR_DELETE2_ACCEPT)]
public class CharacterDelete2AcceptHandler(
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_REQ_CHAR_DELETE2_ACCEPT>
{
    public async Task HandleAsync(CharSessionData session, CH_REQ_CHAR_DELETE2_ACCEPT packet)
    {
        if (!BirthdateMatches(session.Birthdate, packet.BirthDate))
        {
            SendAck(session, packet.CharId, result: 5);
            return;
        }

        try
        {
            var character = await characterRepository.GetByIdAsync((int)packet.CharId);
            if (character is null || character.AccountId != session.AccountId.Value)
            {
                SendAck(session, packet.CharId, result: 3);
                return;
            }

            if (IsDeleteBlockedByBaseLevel(character.BaseLevel, configuration.Char.CharDeleteLevel) ||
                ((configuration.Char.CharDeleteRestriction & 0x01) != 0 && character.PartyId != 0) ||
                ((configuration.Char.CharDeleteRestriction & 0x02) != 0 && character.GuildId != 0))
            {
                SendAck(session, packet.CharId, result: 2);
                return;
            }

            var now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (configuration.Char.CharDeleteDelay > 0 &&
                (character.DeleteDate == 0 || character.DeleteDate > now))
            {
                SendAck(session, packet.CharId, result: 4);
                return;
            }

            await characterRepository.DeleteAsync((int)packet.CharId);
            SendAck(session, packet.CharId, result: 1);
        }
        catch
        {
            SendAck(session, packet.CharId, result: 3);
        }
    }

    internal static bool BirthdateMatches(string sessionBirthdate, byte[] birthdateBytes)
    {
        if (string.IsNullOrEmpty(sessionBirthdate))
        {
            return birthdateBytes.Length > 0 && birthdateBytes[0] == 0;
        }

        var incoming = string.Create(
            8,
            birthdateBytes,
            (span, bytes) =>
            {
                span[0] = (char)bytes[0];
                span[1] = (char)bytes[1];
                span[2] = '-';
                span[3] = (char)bytes[2];
                span[4] = (char)bytes[3];
                span[5] = '-';
                span[6] = (char)bytes[4];
                span[7] = (char)bytes[5];
            });

        var expected = sessionBirthdate.Length >= 2 ? sessionBirthdate[2..] : string.Empty;
        return string.Equals(incoming, expected, StringComparison.Ordinal);
    }

    internal static bool IsDeleteBlockedByBaseLevel(ushort baseLevel, int deleteLevelConfig)
    {
        if (deleteLevelConfig == 0)
        {
            return false;
        }

        if (deleteLevelConfig > 0)
        {
            return baseLevel >= deleteLevelConfig;
        }

        return baseLevel <= -deleteLevelConfig;
    }

    private static void SendAck(CharSessionData session, uint charId, uint result)
    {
        session.EnqueuePacket(new HC_CHAR_DELETE2_ACCEPT_ACK
        {
            CharId = charId,
            Result = result
        });
    }
}
