using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Services;
using Map.Server.Session;
using Map.Server.World;

namespace Map.Server.Handlers;

/// <summary>
/// Post-char-select TCP handshake. rAthena <c>clif_parse_WantToConnection</c>
/// equivalent: validates the auth ticket the char server issued, hydrates the
/// session with the character's saved-state position, and sends
/// <c>ZC_AID</c> + <c>ZC_ACCEPT_ENTER_ZONE</c>. The actual <c>PlayerEntity</c>
/// spawn happens later on <c>CZ_NOTIFY_ACTORINIT</c>
/// (<see cref="NotifyActorInitHandler"/>) — this handler only authenticates.
/// </summary>
[PacketHandler(PacketHeader.CZ_WANT_TO_CONNECTION)]
public class WantToConnectionHandler(
    ICharServerIpcService charServerIpc,
    IMapWorldRegistry worldRegistry,
    MapServerConfiguration configuration,
    ILogger<WantToConnectionHandler> logger
) : IPacketHandler<MapSessionData, CZ_WANT_TO_CONNECTION>
{
    public async Task HandleAsync(MapSessionData session, CZ_WANT_TO_CONNECTION packet)
    {
        if (session.AuthState != MapAuthState.Unauthenticated)
        {
            logger.LogWarning(
                "Replayed CZ_WANT_TO_CONNECTION on session {SessionId} (state {State}) — ignoring",
                session.SessionId, session.AuthState);
            return;
        }

        // rAthena's chrif_authreq sends only login_id1 to the char server; the
        // char server already stored login_id2 with the ticket at char-select.
        // We pass loginId2=0 and rely on MapAuthTicketService to skip that check.
        var auth = await charServerIpc.RequestCharacterMapAuthAsync(
            accountId: packet.AccountId,
            characterId: packet.CharacterId,
            loginId1: packet.LoginId1,
            loginId2: 0,
            sex: packet.Sex,
            ip: 0,
            autotrade: false);

        if (auth?.Success != true)
        {
            logger.LogWarning(
                "Map auth refused for account {AccountId} char {CharId}: {Reason}",
                packet.AccountId, packet.CharacterId, auth?.ErrorMessage ?? "no response");
            session.EnqueuePacket(new ZC_REFUSE_ENTER_ZONE { ErrorCode = 1 });
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        var character = auth.CharacterData;
        var spawnMap = ResolveSpawnMap();
        if (spawnMap == null)
        {
            logger.LogError("No spawn map available — server has no loaded maps");
            session.EnqueuePacket(new ZC_REFUSE_ENTER_ZONE { ErrorCode = 1 });
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }

        var spawnX = character != null && character.PositionX > 0
            ? (short)character.PositionX
            : (short)(spawnMap.Xs / 2);
        var spawnY = character != null && character.PositionY > 0
            ? (short)character.PositionY
            : (short)(spawnMap.Ys / 2);

        session.AccountId = packet.AccountId;
        session.CharacterId = packet.CharacterId;
        session.LoginId1 = packet.LoginId1;
        session.Sex = packet.Sex;
        session.CharacterName = character?.Character?.Name ?? string.Empty;
        session.MapId = (uint)spawnMap.Name.GetHashCode();
        session.SpawnX = spawnX;
        session.SpawnY = spawnY;
        session.SpawnDir = 0;
        session.AuthState = MapAuthState.Authenticated;

        session.EnqueuePacket(new ZC_AID { AccountId = packet.AccountId });
        session.EnqueuePacket(new ZC_ACCEPT_ENTER_ZONE
        {
            StartTime = (uint)Environment.TickCount,
            X = spawnX,
            Y = spawnY,
            Dir = 0,
            Font = 0,
        });

        logger.LogInformation(
            "Map auth accepted for {CharName} (acc {AccountId}, char {CharId}) on {Map} at ({X},{Y})",
            session.CharacterName, packet.AccountId, packet.CharacterId, spawnMap.Name, spawnX, spawnY);
    }

    private MapData? ResolveSpawnMap()
    {
        // Pick the first configured map that's actually loaded. Per-character
        // saved-map resolution lands when we add a shared map-index table
        // (see world.md / session.md history).
        foreach (var name in configuration.Maps)
        {
            var map = worldRegistry.Get(name);
            if (map != null) return map;
        }
        return worldRegistry.All.FirstOrDefault();
    }
}
