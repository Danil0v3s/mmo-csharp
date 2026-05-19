using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;
using Char.Server.Services;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_REQ_CHANGE_CHARNAME)]
public class CharacterRenameApplyHandler(
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_REQ_CHANGE_CHARNAME>
{
    private const byte MinimumCharacterSlots = 3;
    private const byte MaximumCharacterSlots = 9;

    public async Task HandleAsync(CharSessionData session, CH_REQ_CHANGE_CHARNAME packet)
    {
        var character = await characterRepository.GetByIdAsync((int)packet.CharId);
        if (character is null || character.AccountId != session.AccountId.Value)
        {
            return;
        }

        var requestedName = CharacterNamePolicy.Normalize(packet.NewName);
        var targetName = string.IsNullOrEmpty(requestedName)
            ? session.PendingCharacterRename
            : requestedName;

        if (string.IsNullOrWhiteSpace(targetName))
        {
            SendAck(session, 3);
            return;
        }

        if (!CharacterNamePolicy.IsStructurallyValid(targetName, configuration))
        {
            SendAck(session, 8);
            return;
        }

        // rAthena char.cpp:1277 — block rename when the char is in a
        // party / guild unless the corresponding flag allows it.
        if (!configuration.Char.CharRenameParty && character.PartyId != 0)
        {
            SendAck(session, 6);
            return;
        }
        if (!configuration.Char.CharRenameGuild && character.GuildId != 0)
        {
            SendAck(session, 5);
            return;
        }

        if (await CharacterNamePolicy.NameExistsAsync(characterRepository, targetName, configuration))
        {
            SendAck(session, 4);
            return;
        }

        try
        {
            character.Name = targetName;
            await characterRepository.UpdateAsync(character);
        }
        catch
        {
            SendAck(session, 10);
            return;
        }

        session.PendingCharacterRename = string.Empty;
        SendAck(session, 0);
        await ResendCharacterWindowAsync(session);
    }

    private static void SendAck(CharSessionData session, uint result)
    {
        session.EnqueuePacket(new HC_ACK_CHANGE_CHARNAME
        {
            Result = result
        });
    }

    private async Task ResendCharacterWindowAsync(CharSessionData session)
    {
        var characters = await characterRepository.GetByAccountIdAsync(session.AccountId!.Value);
        var activeCharacters = characters.Where(c => c.DeleteDate == 0).ToList();

        var charSlots = session.CharacterSlots > 0
            ? session.CharacterSlots
            : Math.Max(3, activeCharacters.Count);
        var totalPages = Math.Max(charSlots / 3, 1);

        session.EnqueuePacket(new HC_ACCEPT_ENTER2
        {
            Normal = MinimumCharacterSlots,
            Premium = (byte)Math.Clamp(session.VipCharacterSlots, byte.MinValue, byte.MaxValue),
            Billing = (byte)Math.Clamp(session.BillingCharacterSlots, byte.MinValue, byte.MaxValue),
            Producible = (byte)Math.Clamp(charSlots, byte.MinValue, byte.MaxValue),
            Total = MaximumCharacterSlots,
            Extension = string.Empty
        });

        session.EnqueuePacket(new HC_ACCEPT_ENTER
        {
            Total = MaximumCharacterSlots,
            PremiumStart = MinimumCharacterSlots,
            PremiumEnd = (byte)Math.Clamp(MinimumCharacterSlots + session.VipCharacterSlots, byte.MinValue, byte.MaxValue),
            Extension = string.Empty,
            CharacterData = CharacterPacketSerialization.SerializeCharacters(activeCharacters)
        });

        session.EnqueuePacket(new HC_CHARLIST_NOTIFY(PacketHeader.HC_CHARLIST_NOTIFY)
        {
            TotalPages = totalPages,
            CharSlots = charSlots
        });

        session.EnqueuePacket(new HC_BLOCK_CHARACTER
        {
            BlockInfo = Array.Empty<CharacterBlockInfo>()
        });
    }
}
