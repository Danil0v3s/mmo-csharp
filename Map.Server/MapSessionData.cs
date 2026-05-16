using System.Net.Sockets;
using Core.Server.Network;
using Core.Server.Packets;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server;

public class MapSessionData(
    Socket socket,
    int heartbeatTimeout,
    IPacketFactory packetFactory,
    IPacketSizeRegistry sizeRegistry,
    ILogger logger
) : ClientSession(socket, heartbeatTimeout, packetFactory, sizeRegistry, logger)
{
    /// <summary>Auth lifecycle — see <see cref="MapAuthState"/>.</summary>
    public MapAuthState AuthState { get; set; } = MapAuthState.Unauthenticated;

    public int? AccountId { get; set; }
    public int? CharacterId { get; set; }
    public int? LoginId1 { get; set; }
    public byte Sex { get; set; }

    /// <summary>
    /// Internal mapId (hash of map name) the player is currently bound to.
    /// Set when the auth ticket resolves; the spawn happens on
    /// <c>CZ_NOTIFY_ACTORINIT</c>.
    /// </summary>
    public uint? MapId { get; set; }
    public short SpawnX { get; set; }
    public short SpawnY { get; set; }
    public byte SpawnDir { get; set; }
    public string? CharacterName { get; set; }

    /// <summary>
    /// Block-list id of the live <see cref="PlayerEntity"/>, populated once
    /// the player is registered in <see cref="IEntityRegistry"/>. Used by the
    /// disconnect path to find and tear down the entity.
    /// </summary>
    public EntityId? EntityId { get; set; }

    /// <summary>
    /// True once the disconnect cleanup has run. Idempotency guard so the
    /// periodic lifecycle sweep doesn't double-broadcast vanish.
    /// </summary>
    public bool CleanupCompleted { get; set; }
}
