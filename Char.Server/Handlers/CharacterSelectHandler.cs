using Char.Server.Services;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_SELECT_CHAR)]
public class CharacterSelectHandler(
    ILogger<CharacterSelectHandler> logger,
    ICharacterRepository characterRepository,
    IMapAuthTicketService mapAuthTicketService,
    IServerConnectionService serverConnectionService,
    IMapServerRegistryService mapServerRegistry,
    CharServerConfiguration configuration
) : IPacketHandler<CharSessionData, CH_SELECT_CHAR>
{
    private const int MapAuthTicketTtlSeconds = 60;

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
            AccessibleMapCatalog.SendAccessibleMaps(session, mapServerRegistry);
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

        if (!mapServerRegistry.ContainsMap(selectedCharacter.LastMap))
        {
            AccessibleMapCatalog.SendAccessibleMaps(session, mapServerRegistry);
            return;
        }

        if (!mapServerRegistry.TryGetMapAddress(selectedCharacter.LastMap, out var mapIp, out var mapPort))
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
            MapName = selectedCharacter.LastMap,
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

}
