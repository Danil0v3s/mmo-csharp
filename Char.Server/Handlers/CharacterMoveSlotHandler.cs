using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;
using Char.Server.Services;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_MOVE_CHAR_SLOT)]
public class CharacterMoveSlotHandler(
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_MOVE_CHAR_SLOT>
{
    private const int MaxChars = 9;
    private const byte MinimumCharacterSlots = 3;
    private const byte MaximumCharacterSlots = 9;

    public async Task HandleAsync(CharSessionData session, CH_MOVE_CHAR_SLOT packet)
    {
        var from = packet.From;
        var to = packet.To;

        if (from >= MaxChars)
        {
            SendAck(session, reason: 1, moves: 0);
            return;
        }

        var characters = await characterRepository.GetByAccountIdAsync(session.AccountId!.Value);
        var activeCharacters = characters.Where(c => c.DeleteDate == 0).ToList();
        var movesForFromSlot = GetMovesForSlot(activeCharacters, from);

        if (!configuration.CharMove.Enabled)
        {
            SendAck(session, reason: 1, moves: movesForFromSlot);
            return;
        }

        var fromCharacter = activeCharacters.FirstOrDefault(c => c.CharNum == from);
        if (fromCharacter is null)
        {
            SendAck(session, reason: 1, moves: movesForFromSlot);
            return;
        }

        if (!configuration.CharMove.Unlimited && fromCharacter.Moves <= 0)
        {
            SendAck(session, reason: 1, moves: (short)Math.Clamp((int)fromCharacter.Moves, short.MinValue, short.MaxValue));
            return;
        }

        if (to >= MaxChars || to >= session.CharacterSlots)
        {
            SendAck(session, reason: 1, moves: (short)Math.Clamp((int)fromCharacter.Moves, short.MinValue, short.MaxValue));
            return;
        }

        var toCharacter = activeCharacters.FirstOrDefault(c => c.CharNum == to);
        if (toCharacter is not null)
        {
            if (!configuration.CharMove.MoveToUsed)
            {
                SendAck(session, reason: 1, moves: (short)Math.Clamp((int)fromCharacter.Moves, short.MinValue, short.MaxValue));
                return;
            }

            try
            {
                toCharacter.CharNum = (byte)from;
                fromCharacter.CharNum = (byte)to;
                await characterRepository.UpdateAsync(fromCharacter);
                await characterRepository.UpdateAsync(toCharacter);
            }
            catch
            {
                SendAck(session, reason: 1, moves: (short)Math.Clamp((int)fromCharacter.Moves, short.MinValue, short.MaxValue));
                return;
            }
        }
        else
        {
            try
            {
                fromCharacter.CharNum = (byte)to;
                await characterRepository.UpdateAsync(fromCharacter);
            }
            catch
            {
                SendAck(session, reason: 1, moves: (short)Math.Clamp((int)fromCharacter.Moves, short.MinValue, short.MaxValue));
                return;
            }
        }

        if (!configuration.CharMove.Unlimited)
        {
            fromCharacter.Moves--;
            await characterRepository.UpdateAsync(fromCharacter);
        }

        SendAck(session, reason: 0, moves: (short)Math.Clamp((int)fromCharacter.Moves, short.MinValue, short.MaxValue));
        await ResendCharacterWindowAsync(session);
    }

    private static void SendAck(CharSessionData session, short reason, short moves)
    {
        session.EnqueuePacket(new HC_ACK_CHANGE_CHARACTER_SLOT
        {
            Reason = reason,
            CharMoves = moves
        });
    }

    private static short GetMovesForSlot(IReadOnlyCollection<Core.Database.Entities.CharEntity> activeCharacters, ushort slot)
    {
        var character = activeCharacters.FirstOrDefault(c => c.CharNum == slot);
        if (character is null)
        {
            return 0;
        }

        return (short)Math.Clamp((int)character.Moves, short.MinValue, short.MaxValue);
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
