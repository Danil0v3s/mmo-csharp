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
        var spawnMap = SpawnMapResolver.Resolve(worldRegistry, configuration.Maps, character?.MapName);
        if (spawnMap == null)
        {
            logger.LogError("No spawn map available — server has no loaded maps");
            session.EnqueuePacket(new ZC_REFUSE_ENTER_ZONE { ErrorCode = 1 });
            session.Disconnect(DisconnectReason.Kicked);
            return;
        }
        if (!string.IsNullOrEmpty(character?.MapName)
            && !MapNamesMatch(character.MapName, spawnMap.Name))
        {
            logger.LogWarning(
                "Character's saved map '{Saved}' not loaded on this server; falling back to {Fallback}",
                character.MapName, spawnMap.Name);
        }

        // rAthena pc_setpos (pc.cpp:6949-6967): if requested coords are out
        // of the loaded map's bounds, zero them and pick a random walkable
        // cell. The captured rAthena spawning at iz_int03,103,89 (the
        // configured start point) lands at a random (x, y) because iz_int03
        // is 80x80 — those coords are OOB.
        var savedX = character?.PositionX > 0 ? (int)character.PositionX : -1;
        var savedY = character?.PositionY > 0 ? (int)character.PositionY : -1;
        var inBounds = savedX > 0 && savedY > 0 && savedX < spawnMap.Xs && savedY < spawnMap.Ys;
        short spawnX, spawnY;
        if (inBounds && spawnMap.IsWalkable((short)savedX, (short)savedY))
        {
            spawnX = (short)savedX;
            spawnY = (short)savedY;
        }
        else
        {
            (spawnX, spawnY) = PickRandomWalkableCell(spawnMap);
            if (savedX > 0 && savedY > 0)
            {
                logger.LogWarning(
                    "pc_setpos: invalid spawn ({SavedX},{SavedY}) on {Map} ({Xs}x{Ys}); randomized to ({X},{Y})",
                    savedX, savedY, spawnMap.Name, spawnMap.Xs, spawnMap.Ys, spawnX, spawnY);
            }
        }

        session.AccountId = packet.AccountId;
        session.CharacterId = packet.CharacterId;
        session.LoginId1 = packet.LoginId1;
        session.Sex = packet.Sex;
        session.GroupId = auth.GroupId;
        session.CharacterName = character?.Character?.Name ?? string.Empty;
        session.MapId = (uint)spawnMap.Name.GetHashCode();
        session.SpawnX = spawnX;
        session.SpawnY = spawnY;
        session.SpawnDir = 0;
        session.AuthState = MapAuthState.Authenticated;

        session.EnqueuePacket(new ZC_AID { AccountId = packet.AccountId });

        // rAthena pc_authok emits clif_inventory_expansion_info between
        // ZC_AID and ZC_ACCEPT_ENTER (pc.cpp:2168). For a fresh character
        // the expansion size is 0 (inventory_slots == INVENTORY_BASE_SIZE);
        // the client still expects the packet on the wire.
        session.EnqueuePacket(new ZC_EXTEND_BODYITEM_SIZE { ExpansionSize = 0 });

        session.EnqueuePacket(new ZC_ACCEPT_ENTER_ZONE
        {
            StartTime = (uint)Environment.TickCount,
            X = spawnX,
            Y = spawnY,
            Dir = 0,
            Font = 0,
        });

        // rAthena pc_authok emits clif_friendslist_send right after
        // clif_authok (pc.cpp:2182). For now we always send an empty list
        // — the friends table is loaded lazily on `clif_friendslist_toggle`
        // when an online friend logs in, not from the char data here.
        session.EnqueuePacket(new ZC_FRIENDS_LIST());

        // rAthena pc_authok next: version string (pc_show_version, pc.cpp:2186)
        // then MOTD lines (pc.cpp:2190-2195), then clif_changemap
        // (pc.cpp:2203). Skip the !changing_mapservers block when this is
        // a server-to-server warp; for fresh logins (always the case for
        // CZ_WANT_TO_CONNECTION today) we emit all three.
        if (configuration.DisplayVersion && !string.IsNullOrEmpty(configuration.VersionMessage))
        {
            session.EnqueuePacket(new ZC_NOTIFY_PLAYERCHAT { Message = configuration.VersionMessage });
        }
        foreach (var line in configuration.MotdLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
            session.EnqueuePacket(new ZC_NOTIFY_PLAYERCHAT { Message = line });
        }

        session.EnqueuePacket(new ZC_NPCACK_MAPMOVE
        {
            MapName = spawnMap.Name,
            X = spawnX,
            Y = spawnY,
        });

        logger.LogInformation(
            "Map auth accepted for {CharName} (acc {AccountId}, char {CharId}) on {Map} at ({X},{Y})",
            session.CharacterName, packet.AccountId, packet.CharacterId, spawnMap.Name, spawnX, spawnY);
    }

    /// <summary>
    /// Mirrors rAthena pc_setpos's random-cell selection (pc.cpp:6955-6967):
    /// roll random (x, y) in [1, xs-2] × [1, ys-2] and resample until the
    /// cell is walkable. Bounded retry count to prevent an infinite loop on
    /// fully-blocked maps; falls back to (xs/2, ys/2) if every attempt fails.
    /// </summary>
    private static (short X, short Y) PickRandomWalkableCell(World.MapData map)
    {
        var maxAttempts = map.Xs * map.Ys * 3;
        for (var i = 0; i < maxAttempts; i++)
        {
            var x = (short)(Random.Shared.Next(map.Xs - 2) + 1);
            var y = (short)(Random.Shared.Next(map.Ys - 2) + 1);
            if (map.IsWalkable(x, y)) return (x, y);
        }
        return ((short)(map.Xs / 2), (short)(map.Ys / 2));
    }

    private static bool MapNamesMatch(string a, string b)
    {
        if (a.EndsWith(".gat", StringComparison.OrdinalIgnoreCase)) a = a[..^4];
        if (b.EndsWith(".gat", StringComparison.OrdinalIgnoreCase)) b = b[..^4];
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
