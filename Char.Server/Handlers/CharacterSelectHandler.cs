using Char.Server.Services;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;
using System.Net;
using System.Net.Sockets;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_SELECT_CHAR)]
public class CharacterSelectHandler(
    ILogger<CharacterSelectHandler> logger,
    ICharacterRepository characterRepository,
    IMapAuthTicketService mapAuthTicketService,
    IServerConnectionService serverConnectionService,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_SELECT_CHAR>
{
    private const int MapAuthTicketTtlSeconds = 60;
    private const string FallbackMapName = "prontera";
    private const string MapEndpointKey = "MapServer";

    public async Task HandleAsync(CharSessionData session, CH_SELECT_CHAR packet)
    {
        if (!session.AccountId.HasValue || !session.IsAuthenticated || !session.AccountDataLoaded)
        {
            logger.LogWarning("Rejecting CH_SELECT_CHAR from unauthenticated session {SessionId}", session.SessionId);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        if (configuration.Pincode.Enabled && !session.PincodeVerified)
        {
            PincodeFlowSupport.SendState(
                session,
                string.IsNullOrEmpty(session.Pincode) ? PincodeState.New : PincodeState.Ask);
            return;
        }

        if (!serverConnectionService.HasConnection(ServerType.Map))
        {
            logger.LogWarning(
                "Rejecting CH_SELECT_CHAR for account {AccountId}: no active map-server connection",
                session.AccountId.Value);
            CharRejectFlow.RejectAuthResult(session, resultCode: 1, disconnect: false);
            return;
        }

        logger.LogInformation(
            "Character select request from account {AccountId}, slot {Slot} (session {SessionId})",
            session.AccountId.Value,
            packet.Slot,
            session.SessionId);

        IReadOnlyList<CharEntity> characters;
        try
        {
            characters = await characterRepository.GetByAccountIdAsync(session.AccountId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed loading characters for CH_SELECT_CHAR (account {AccountId})", session.AccountId.Value);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        if (!TrySelectCharacterForSlot(characters, session.AccountId.Value, packet.Slot, out var selectedCharacter))
        {
            logger.LogWarning(
                "Rejecting CH_SELECT_CHAR for account {AccountId}: slot {Slot} not found/invalid",
                session.AccountId.Value,
                packet.Slot);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        selectedCharacter.Online = -2;
        try
        {
            await characterRepository.UpdateAsync(selectedCharacter);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed marking selected character {CharacterId} as online for account {AccountId}",
                selectedCharacter.CharId,
                session.AccountId.Value);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        if (!TryResolveMapEndpoint(configuration, out var mapIp, out var mapPort))
        {
            logger.LogWarning(
                "Rejecting CH_SELECT_CHAR for account {AccountId}: map endpoint could not be resolved",
                session.AccountId.Value);
            CharRejectFlow.RejectAuthResult(session, resultCode: 1, disconnect: false);
            return;
        }

        var ticketIssued = mapAuthTicketService.IssueTicket(
            session.AccountId.Value,
            selectedCharacter.CharId,
            session.LoginId1,
            session.LoginId2,
            session.Sex,
            session.ClientType,
            MapAuthTicketTtlSeconds);

        if (!ticketIssued)
        {
            logger.LogWarning(
                "Rejecting CH_SELECT_CHAR for account {AccountId}: map auth ticket issuance failed (char {CharacterId})",
                session.AccountId.Value,
                selectedCharacter.CharId);
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        session.CharacterId = selectedCharacter.CharId;

        var responsePacket = new HC_SEND_MAP_DATA
        {
            CharId = (uint)selectedCharacter.CharId,
            MapName = ResolveDestinationMapName(selectedCharacter, configuration),
            Ip = mapIp,
            Port = mapPort,
            Domain = string.Empty
        };

        session.EnqueuePacket(responsePacket);
    }

    internal static bool TrySelectCharacterForSlot(
        IReadOnlyList<CharEntity> characters,
        int accountId,
        byte slot,
        out CharEntity selectedCharacter)
    {
        selectedCharacter = characters.FirstOrDefault(c =>
            c.AccountId == accountId &&
            c.CharNum == slot &&
            c.DeleteDate == 0)!;

        return selectedCharacter != null;
    }

    internal static string ResolveDestinationMapName(CharEntity character, CharServerConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(character.LastMap))
        {
            return character.LastMap;
        }

        if (!string.IsNullOrWhiteSpace(character.SaveMap))
        {
            return character.SaveMap;
        }

        var configuredStart = configuration.StartPoint.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Map));
        if (!string.IsNullOrWhiteSpace(configuredStart?.Map))
        {
            return configuredStart.Map;
        }

        return FallbackMapName;
    }

    internal static bool TryResolveMapEndpoint(CharServerConfiguration configuration, out uint ip, out ushort port)
    {
        ip = 0;
        port = 0;

        if (TryConvertIpv4ToUInt(configuration.MapIp, out var configuredIp) && configuration.MapPort > 0)
        {
            ip = configuredIp;
            port = configuration.MapPort;
            return true;
        }

        if (configuration.OtherServerEndpoints.TryGetValue(MapEndpointKey, out var endpoint) &&
            Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            if (TryConvertIpv4ToUInt(uri.Host, out var hostIp))
            {
                ip = hostIp;
                port = (ushort)uri.Port;
                return port > 0;
            }

            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                TryConvertIpv4ToUInt("127.0.0.1", out var loopbackIp))
            {
                ip = loopbackIp;
                port = (ushort)uri.Port;
                return port > 0;
            }
        }

        return false;
    }

    internal static bool TryConvertIpv4ToUInt(string ipAddress, out uint ip)
    {
        ip = 0;
        if (!IPAddress.TryParse(ipAddress, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = parsed.GetAddressBytes();
        ip = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        return true;
    }
}
