using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;
using HcCharacterInfo = Core.Server.Packets.Out.HC.CharacterInfo;
using Char.Server.Services;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_MAKE_NEW_CHAR)]
public class CharacterCreateHandler(
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_MAKE_NEW_CHAR>
{
    public async Task HandleAsync(CharSessionData session, CH_MAKE_NEW_CHAR packet)
    {
        if (!configuration.CharNew)
        {
            SendRefuse(session, -2);
            return;
        }

        if (!session.AccountId.HasValue)
        {
            return;
        }

        var createResult = await TryCreateCharacterAsync(session, packet);
        if (!createResult.Success)
        {
            SendRefuse(session, createResult.ErrorCode);
            return;
        }

        session.EnqueuePacket(new HC_ACCEPT_MAKECHAR
        {
            CharData = SerializeCharacter(createResult.Character!)
        });
    }

    private async Task<(bool Success, int ErrorCode, CharEntity? Character)> TryCreateCharacterAsync(
        CharSessionData session,
        CH_MAKE_NEW_CHAR packet)
    {
        var characterName = CharacterNamePolicy.Normalize(packet.Name);

        if (!CharacterNamePolicy.IsStructurallyValid(characterName, configuration))
        {
            return (false, -2, null);
        }

        if (await CharacterNamePolicy.NameExistsAsync(characterRepository, characterName, configuration))
        {
            return (false, -1, null);
        }

        var characters = await characterRepository.GetByAccountIdAsync(session.AccountId!.Value);
        var activeCharacters = characters.Where(c => c.DeleteDate == 0).ToList();

        if (packet.Slot >= session.CharacterSlots ||
            activeCharacters.Any(c => c.CharNum == packet.Slot))
        {
            return (false, -4, null);
        }

        var startPoint = configuration.StartPoint.FirstOrDefault() ?? new StartPoint { Map = "prontera", X = 156, Y = 191 };
        var character = new CharEntity
        {
            AccountId = session.AccountId.Value,
            CharNum = packet.Slot,
            Name = characterName,
            Class = (ushort)Math.Min(packet.StartingJob, ushort.MaxValue),
            BaseLevel = 1,
            JobLevel = 1,
            Str = 1,
            Agi = 1,
            Vit = 1,
            Int = 1,
            Dex = 1,
            Luk = 1,
            Hair = (byte)Math.Min(packet.HairStyle, byte.MaxValue),
            HairColor = packet.HairColor,
            Sex = packet.Sex == 0 ? "F" : "M",
            LastMap = startPoint.Map,
            LastX = (ushort)startPoint.X,
            LastY = (ushort)startPoint.Y,
            SaveMap = startPoint.Map,
            SaveX = (ushort)startPoint.X,
            SaveY = (ushort)startPoint.Y,
            Online = 0,
            DeleteDate = 0,
            LastLogin = DateTime.UtcNow
        };

        try
        {
            character = await characterRepository.AddAsync(character);
            return (true, 0, character);
        }
        catch
        {
            return (false, -2, null);
        }
    }

    private static void SendRefuse(CharSessionData session, int errorCode)
    {
        var packet = new HC_REFUSE_MAKECHAR();
        switch (errorCode)
        {
            case -1:
                packet = new HC_REFUSE_MAKECHAR { Error = 0x00 };
                break;
            case -2:
                packet = new HC_REFUSE_MAKECHAR { Error = 0xFF };
                break;
            case -3:
                packet = new HC_REFUSE_MAKECHAR { Error = 0x01 };
                break;
            case -4:
                packet = new HC_REFUSE_MAKECHAR { Error = 0x03 };
                break;
        }

        session.EnqueuePacket(packet);
    }

    private static byte[] SerializeCharacter(CharEntity character)
    {
        var info = new HcCharacterInfo
        {
            CharId = character.CharId,
            Exp = (long)Math.Min(character.BaseExp, (ulong)long.MaxValue),
            Zeny = (int)Math.Min(character.Zeny, (uint)int.MaxValue),
            JobLevel = (short)Math.Min(character.JobLevel, (ushort)short.MaxValue),
            Name = character.Name
        };

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        info.Write(writer);
        return ms.ToArray();
    }
}
