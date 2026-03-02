using Char.Server.Services;
using Core.Database.Repositories.Api;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Core.Server.Packets.Out.HC;

namespace Char.Server.Handlers;

[PacketHandler(PacketHeader.CH_SELECT_ACCESSIBLE_MAPNAME)]
public class CharacterSelectAccessibleMapHandler(
    ICharacterRepository characterRepository,
    IMapAuthTicketService mapAuthTicketService,
    IServerConnectionService serverConnectionService,
    IMapServerRegistryService mapServerRegistry
) : IPacketHandler<CharSessionData, CH_SELECT_ACCESSIBLE_MAPNAME>
{
    private const int MapAuthTicketTtlSeconds = 60;

    public async Task HandleAsync(CharSessionData session, CH_SELECT_ACCESSIBLE_MAPNAME packet)
    {
        if (!session.AccountId.HasValue || !session.IsAuthenticated || !session.AccountDataLoaded)
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        var characters = await characterRepository.GetByAccountIdAsync(session.AccountId.Value);
        if (!CharacterSelectHandler.TrySelectCharacterForSlot(characters, session.AccountId.Value, (byte)packet.Slot, out var selectedCharacter))
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        selectedCharacter.Online = -2;
        try
        {
            await characterRepository.UpdateAsync(selectedCharacter);
        }
        catch
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        // rAthena parity: CH_SELECT_ACCESSIBLE_MAPNAME is valid only when current last map has no map-server.
        if (mapServerRegistry.ContainsMap(selectedCharacter.LastMap))
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        if ((uint)packet.MapNumber >= AccessibleMapCatalog.Maps.Length)
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        var chosenMap = AccessibleMapCatalog.Maps[packet.MapNumber];
        if (!mapServerRegistry.ContainsMap(chosenMap.Map))
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        selectedCharacter.LastMap = chosenMap.Map;
        selectedCharacter.LastX = (ushort)chosenMap.X;
        selectedCharacter.LastY = (ushort)chosenMap.Y;

        try
        {
            await characterRepository.UpdateAsync(selectedCharacter);
        }
        catch
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        if (!serverConnectionService.HasConnection(ServerType.Map))
        {
            CharRejectFlow.RejectAuthResult(session, resultCode: 1, disconnect: false);
            return;
        }

        if (!mapServerRegistry.TryGetMapAddress(chosenMap.Map, out var mapIp, out var mapPort))
        {
            CharRejectFlow.RejectAuthResult(session, resultCode: 1, disconnect: false);
            return;
        }

        if (!mapAuthTicketService.IssueTicket(
                session.AccountId.Value,
                selectedCharacter.CharId,
                session.LoginId1,
                session.LoginId2,
                session.Sex,
                session.ClientType,
                MapAuthTicketTtlSeconds))
        {
            CharRejectFlow.RejectEnter(session, errorCode: 0);
            return;
        }

        session.CharacterId = selectedCharacter.CharId;
        session.EnqueuePacket(new HC_SEND_MAP_DATA
        {
            CharId = (uint)selectedCharacter.CharId,
            MapName = chosenMap.Map,
            Ip = mapIp,
            Port = mapPort,
            Domain = string.Empty
        });
    }
}
