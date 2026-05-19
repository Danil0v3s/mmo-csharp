using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;
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
            CharData = CharacterPacketSerialization.SerializeCharacter(createResult.Character!)
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

        // rAthena allowed_job_flag (char.cpp:1481, PACKETVER ≥ 20151001):
        //   bit 0 → JOB_NOVICE (id 0) allowed
        //   bit 1 → JOB_SUMMONER (id 4218) allowed
        // Default mask is 3 (both). Other job ids are never legal here —
        // the client only ships novice / summoner via the create dialog.
        if (!IsJobAllowed(packet.StartingJob, configuration.AllowedJobFlag))
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

        // rAthena `make_new_char_sql` formulas (char.cpp:1496-1501):
        //   max_hp = hp = 40 * (100 + vit) / 100
        //   max_sp = sp = 11 * (100 + int) / 100
        // With the starting Vit=1, Int=1 these collapse to 40/11 — the same
        // values the captured rAthena writes into HC_ACCEPT_MAKECHAR.
        const byte StartStr = 1, StartAgi = 1, StartVit = 1, StartInt = 1, StartDex = 1, StartLuk = 1;
        var maxHp = (uint)(40 * (100 + StartVit) / 100);
        var maxSp = (uint)(11 * (100 + StartInt) / 100);

        var character = new CharEntity
        {
            AccountId = session.AccountId.Value,
            CharNum = packet.Slot,
            Name = characterName,
            Class = (ushort)Math.Min(packet.StartingJob, ushort.MaxValue),
            BaseLevel = 1,
            JobLevel = 1,
            Str = StartStr,
            Agi = StartAgi,
            Vit = StartVit,
            Int = StartInt,
            Dex = StartDex,
            Luk = StartLuk,
            StatusPoint = configuration.StartStatusPoints,
            Zeny = (uint)Math.Max(configuration.StartZeny, 0),
            MaxHp = maxHp,
            Hp = maxHp,
            MaxSp = maxSp,
            Sp = maxSp,
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

    private const ushort JobNovice = 0;
    private const ushort JobSummoner = 4218;

    internal static bool IsJobAllowed(uint startingJob, int allowedJobFlag)
    {
        // -1 is the C#-config sentinel meaning "no gate" (legacy default
        // before this gate landed). rAthena's default is 3 (novice +
        // summoner); 0 disables both.
        if (allowedJobFlag < 0) return true;
        if (startingJob == JobNovice) return (allowedJobFlag & 1) != 0;
        if (startingJob == JobSummoner) return (allowedJobFlag & 2) != 0;
        // rAthena returns -2 (Invalid job) for anything not in the
        // novice/summoner pair; the client never offers other ids in
        // the create dialog anyway.
        return false;
    }
}
