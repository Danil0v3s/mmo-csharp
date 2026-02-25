using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Services;

public interface ICharacterListFlowService
{
    Task<bool> TrySendInitialCharacterWindowAsync(CharSessionData session, CancellationToken cancellationToken = default);
    Task<bool> TrySendCharacterPageAsync(CharSessionData session, CancellationToken cancellationToken = default);
}

public class CharacterListFlowService(
    ILogger<CharacterListFlowService> logger,
    ICharacterRepository characterRepository,
    CharServerConfiguration configuration,
    SessionManager sessionManager
) : ICharacterListFlowService
{
    private const int DefaultCharSlots = 3;
    private const byte MinimumCharacterSlots = 3;
    private const byte MaximumCharacterSlots = 9;

    public async Task<bool> TrySendInitialCharacterWindowAsync(CharSessionData session, CancellationToken cancellationToken = default)
    {
        if (!TryValidateSessionForCharacterFlow(session))
        {
            return false;
        }

        if (!TryValidateCapacityAndMaintenance(session, out var connectedUsers))
        {
            logger.LogInformation(
                "Rejecting initial char window for account {AccountId} due to maintenance/capacity (maintenance={Maintenance}, connected={ConnectedUsers}, max={MaxUsers}, group={GroupId}, bypassGroup={BypassGroup})",
                session.AccountId!.Value,
                configuration.CharMaintenance == 1,
                connectedUsers,
                configuration.MaxConnectUser,
                session.GroupId,
                configuration.GmAllowGroup);
            CharRejectFlow.RejectAuthResult(session, resultCode: 7);
            return false;
        }

        var initialLoad = await TryLoadActiveCharacters(session, cancellationToken);
        if (!initialLoad.Success)
        {
            return false;
        }
        var listPayload = initialLoad.Payload;
        var charSlots = initialLoad.CharSlots;

        var totalPages = Math.Max(charSlots / 3, 1);

        // rAthena connect continuation parity:
        // 0x082d -> 0x006b -> 0x09a0 -> 0x020d
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
            CharacterData = Array.Empty<byte>()
        });

        session.EnqueuePacket(new HC_CHARLIST_NOTIFY(PacketHeader.HC_CHARLIST_NOTIFY)
        {
            TotalPages = totalPages,
            CharSlots = charSlots
        });

        session.EnqueuePacket(new HC_BLOCK_CHARACTER
        {
            PacketLength = sizeof(short) + sizeof(short),
            BlockInfo = Array.Empty<CharacterBlockInfo>()
        });

        if (configuration.Pincode.Enabled && !session.PincodeVerified)
        {
            PincodeFlowSupport.SendState(
                session,
                string.IsNullOrEmpty(session.Pincode) ? PincodeState.New : PincodeState.Ask);
        }

        logger.LogInformation(
            "Sent initial character window payload for account {AccountId} (session {SessionId}) (chars={CharacterCount})",
            session.AccountId.Value,
            session.SessionId,
            listPayload.Length);

        return true;
    }

    public async Task<bool> TrySendCharacterPageAsync(CharSessionData session, CancellationToken cancellationToken = default)
    {
        if (!TryValidateSessionForCharacterFlow(session))
        {
            logger.LogWarning("Rejecting charlist flow from unauthenticated session {SessionId}", session.SessionId);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return false;
        }

        if (configuration.Pincode.Enabled && !session.PincodeVerified)
        {
            PincodeFlowSupport.SendState(
                session,
                string.IsNullOrEmpty(session.Pincode) ? PincodeState.New : PincodeState.Ask);
            return false;
        }

        if (!TryValidateCapacityAndMaintenance(session, out var connectedUsers))
        {
            logger.LogInformation(
                "Rejecting char page request for account {AccountId} due to maintenance/capacity (maintenance={Maintenance}, connected={ConnectedUsers}, max={MaxUsers}, group={GroupId}, bypassGroup={BypassGroup})",
                session.AccountId.Value,
                configuration.CharMaintenance == 1,
                connectedUsers,
                configuration.MaxConnectUser,
                session.GroupId,
                configuration.GmAllowGroup);
            // rAthena uses auth-result overpopulated signaling.
            CharRejectFlow.RejectAuthResult(session, resultCode: 7);
            return false;
        }

        var pageLoad = await TryLoadActiveCharacters(session, cancellationToken);
        if (!pageLoad.Success)
        {
            return false;
        }
        var listPayload = pageLoad.Payload;

        // rAthena CH_REQ_CHARLIST equivalent (0x099d-like per-page ack path).
        session.EnqueuePacket(new HC_ACK_CHARINFO_PER_PAGE
        {
            CharInfoData = SerializeCharacterPageData(listPayload)
        });

        logger.LogInformation(
            "Sent character page payload for account {AccountId} (session {SessionId})",
            session.AccountId.Value,
            session.SessionId);

        return true;
    }

    internal static bool IsBlockedByMaintenanceOrCapacity(
        int charMaintenance,
        int maxConnectUser,
        int connectedUsers,
        int groupId,
        int gmAllowGroup)
    {
        var inMaintenance = charMaintenance == 1;
        var capacityReached = maxConnectUser == 0 ||
                              (maxConnectUser > 0 && connectedUsers >= maxConnectUser);
        var bypassByGroup = groupId >= gmAllowGroup;
        return (inMaintenance || capacityReached) && !bypassByGroup;
    }

    internal static bool IsOutOfOrderCharlistRequest(bool isAuthenticated, bool accountDataLoaded)
    {
        return !isAuthenticated || !accountDataLoaded;
    }

    private bool TryValidateSessionForCharacterFlow(CharSessionData session)
    {
        if (!session.AccountId.HasValue || IsOutOfOrderCharlistRequest(session.IsAuthenticated, session.AccountDataLoaded))
        {
            logger.LogWarning("Rejecting character flow from unauthenticated/out-of-order session {SessionId}", session.SessionId);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return false;
        }

        return true;
    }

    private bool TryValidateCapacityAndMaintenance(CharSessionData session, out int connectedUsers)
    {
        connectedUsers = sessionManager.GetAllSessions()
            .OfType<CharSessionData>()
            .Where(s => s.AccountId.HasValue && s.IsAuthenticated)
            .Select(s => s.AccountId!.Value)
            .Distinct()
            .Count();

        return !IsBlockedByMaintenanceOrCapacity(
            configuration.CharMaintenance,
            configuration.MaxConnectUser,
            connectedUsers,
            session.GroupId,
            configuration.GmAllowGroup);
    }

    private async Task<(bool Success, Core.Server.Packets.Out.HC.CharacterInfo[] Payload, int CharSlots)> TryLoadActiveCharacters(
        CharSessionData session,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CharEntity> characters;
        try
        {
            characters = await characterRepository.GetByAccountIdAsync(session.AccountId!.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed loading character list from database for account {AccountId} (session {SessionId})",
                session.AccountId.Value,
                session.SessionId);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return (false, Array.Empty<Core.Server.Packets.Out.HC.CharacterInfo>(), DefaultCharSlots);
        }

        var activeCharacters = characters
            .Where(c => c.DeleteDate == 0)
            .OrderBy(c => c.CharNum)
            .ToList();

        var listPayload = activeCharacters
            .Select(c => new Core.Server.Packets.Out.HC.CharacterInfo
            {
                CharId = c.CharId,
                Exp = (long)Math.Min(c.BaseExp, (ulong)long.MaxValue),
                Zeny = (int)Math.Min(c.Zeny, (uint)int.MaxValue),
                JobLevel = (short)Math.Min(c.JobLevel, (ushort)short.MaxValue),
                Name = c.Name
            })
            .ToArray();

        var charSlots = session.CharacterSlots > 0
            ? session.CharacterSlots
            : Math.Max(DefaultCharSlots, activeCharacters.Count);

        return (true, listPayload, charSlots);
    }

    private static byte[] SerializeCharacterPageData(Core.Server.Packets.Out.HC.CharacterInfo[] characters)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        foreach (var character in characters)
        {
            character.Write(writer);
        }

        return ms.ToArray();
    }
}
