using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CA;
using Map.Server.Session;

namespace Map.Server.Handlers;

/// <summary>
/// rAthena's map server doesn't read <c>CA_EXE_HASHCHECK</c> — it's a
/// CA_ packet scoped to the login server, where <c>chrif_exe_hashcheck</c>
/// stashes the client integrity hash on the auth node. Some clients
/// (DHXJ / 20220401 included) re-fire it opportunistically after spawn
/// as an anti-cheat heartbeat. Without a handler, the Core dispatcher's
/// default "unknown packet → disconnect" policy would kick the client
/// the next time it pings.
///
/// We accept and drop the packet — the login server already captured
/// the hash during auth. No state mutation, no broadcast.
/// </summary>
[PacketHandler(PacketHeader.CA_EXE_HASHCHECK)]
public class ExeHashCheckIgnoreHandler : IPacketHandler<MapSessionData, CA_EXE_HASHCHECK>
{
    public Task HandleAsync(MapSessionData session, CA_EXE_HASHCHECK packet)
        => Task.CompletedTask;
}
