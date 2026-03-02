using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_DELETE_CHAR)]
public class CharacterDeleteHandler(
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_DELETE_CHAR>
{
    public async Task HandleAsync(CharSessionData session, CH_DELETE_CHAR packet)
    {
        if (!session.AccountId.HasValue)
        {
            return;
        }

        if (!DeleteKeyMatches(session, packet.Key, configuration.Char.CharDeleteOption))
        {
            session.EnqueuePacket(new HC_REFUSE_DELETECHAR { Error = 0x00 });
            return;
        }

        var character = await characterRepository.GetByIdAsync((int)packet.CharId);
        if (character is null || character.AccountId != session.AccountId.Value || character.DeleteDate != 0)
        {
            session.EnqueuePacket(new HC_REFUSE_DELETECHAR { Error = 0x01 });
            return;
        }

        if (IsDeleteBlockedByBaseLevel(character.BaseLevel, configuration.Char.CharDeleteLevel))
        {
            session.EnqueuePacket(new HC_REFUSE_DELETECHAR { Error = 0x00 });
            return;
        }

        if ((configuration.Char.CharDeleteRestriction & 0x02) != 0 && character.GuildId != 0)
        {
            session.EnqueuePacket(new HC_REFUSE_DELETECHAR { Error = 0x02 });
            return;
        }

        if ((configuration.Char.CharDeleteRestriction & 0x01) != 0 && character.PartyId != 0)
        {
            session.EnqueuePacket(new HC_REFUSE_DELETECHAR { Error = 0x02 });
            return;
        }

        // Keep char-list visibility parity with existing code paths (soft-delete marker).
        character.DeleteDate = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        character.Online = 0;

        try
        {
            await characterRepository.UpdateAsync(character);
        }
        catch
        {
            session.EnqueuePacket(new HC_REFUSE_DELETECHAR { Error = 0x00 });
            return;
        }

        session.EnqueuePacket(new HC_ACCEPT_DELETECHAR());
    }

    internal static bool DeleteKeyMatches(CharSessionData session, string deleteKey, int deleteOption)
    {
        if ((deleteOption & 0x01) != 0)
        {
            var emailMatches = string.Equals(deleteKey, session.Email, StringComparison.OrdinalIgnoreCase) ||
                               (string.Equals(session.Email, "a@a.com", StringComparison.OrdinalIgnoreCase) &&
                                string.IsNullOrEmpty(deleteKey));
            if (emailMatches)
            {
                return true;
            }
        }

        if ((deleteOption & 0x02) != 0)
        {
            var sessionBirthdateSuffix = session.Birthdate.Length >= 2 ? session.Birthdate[2..] : string.Empty;
            var birthdateMatches = string.Equals(sessionBirthdateSuffix, deleteKey, StringComparison.Ordinal) ||
                                   (string.IsNullOrEmpty(session.Birthdate) && string.IsNullOrEmpty(deleteKey));
            if (birthdateMatches)
            {
                return true;
            }
        }

        return false;
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
}
