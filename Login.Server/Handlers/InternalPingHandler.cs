using Core.Server;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In;
using Core.Server.Packets.Out;

namespace Login.Server.Handlers;

/// <summary>
/// Replies to <see cref="CZ_INTERNAL_PING"/> with the server's current
/// readiness state. Used by the test harness to wait for the stack to
/// finish booting instead of scraping log files.
/// </summary>
[PacketHandler(PacketHeader.CZ_INTERNAL_PING)]
public class InternalPingHandler(IServerReadiness readiness) : IPacketHandler<LoginSessionData, CZ_INTERNAL_PING>
{
    public Task HandleAsync(LoginSessionData session, CZ_INTERNAL_PING packet)
    {
        session.EnqueuePacket(new ZC_INTERNAL_PONG { Ready = (byte)(readiness.IsReady ? 1 : 0) });
        return Task.CompletedTask;
    }
}
