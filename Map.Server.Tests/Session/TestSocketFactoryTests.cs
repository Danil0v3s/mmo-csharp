using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Session;

/// <summary>
/// Regression coverage for the GC-finalization flake that surfaced as
/// the "Collection: []" outbound-queue failures in trade / storage
/// tests under <c>--filter ~Trade</c> / <c>~Storage</c>.
///
/// <para>
/// The mechanism: <see cref="TestSocketFactory.CreateSocketPair"/>
/// previously returned a <c>SocketPair</c> whose <c>ClientSide</c> was
/// only rooted through the caller's local variable. When that local
/// went out of scope (typical pattern: <c>AddPc</c> returns after
/// capturing <c>ServerSide</c> on a session and discarding the pair),
/// the client socket became collectible. Under parallel-test GC
/// pressure it would get finalized, closing the loopback peer; the
/// session's <c>ReceiveLoopAsync</c> then read 0 bytes, called
/// <c>Disconnect()</c>, and flipped <c>IsAlive=false</c>. From that
/// moment on, <c>EnqueuePacket</c> silently no-ops, and any test
/// asserting on the outbound queue past that point saw an empty
/// collection with no other hint of what went wrong.
/// </para>
///
/// <para>
/// The fix roots every pair in a process-lifetime
/// <see cref="System.Collections.Concurrent.ConcurrentBag{T}"/> so the
/// client side cannot be finalized while a test is mid-flow. This test
/// reproduces the pre-fix mechanism: discards the pair reference,
/// induces full GC + finalization, then asserts the session is still
/// alive and enqueueing.
/// </para>
/// </summary>
public class TestSocketFactoryTests
{
    [Fact]
    public void SocketPair_SurvivesFullGcAndFinalization()
    {
        // Spawn a session the same way handler tests do, then drop every
        // reference to the SocketPair record.
        var session = CreateSessionDiscardingPair();

        // Give the receive loop a moment to start.
        Thread.Sleep(20);

        // Two full GC cycles + finalizer drain — what xUnit's parallel
        // collector eventually triggers under load. Pre-fix: the client
        // socket would be finalized here, the loopback peer would close,
        // and the receive loop would set IsAlive=false within tens of ms.
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(20);
        }

        // The session must still be alive (the regression: this flipped
        // to false when the client socket got collected) and the outbound
        // queue must accept new packets.
        Assert.True(session.IsAlive, "session went dead during GC — the loopback peer was finalized");

        session.EnqueuePacket(new ZC_NOTIFY_PLAYERCHAT { Message = "post-gc ping" });

        var queueField = typeof(ClientSession).GetField(
            "_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var queue = queueField!.GetValue(session) as System.Collections.Concurrent.ConcurrentQueue<byte[]>;
        Assert.NotNull(queue);
        Assert.NotEmpty(queue!);
    }

    private static ClientSession CreateSessionDiscardingPair()
    {
        var packets = new PacketSystem();
        var pair = TestSocketFactory.CreateSocketPair();
        var session = new ClientSession(
            pair.ServerSide,
            heartbeatTimeout: 30_000,
            packetFactory: packets.Factory,
            sizeRegistry: packets.Registry,
            logger: NullLogger.Instance);
        // Intentionally drop the pair reference here — only the ServerSide
        // survives via the session. Pre-fix this is exactly the scenario
        // that lost the ClientSide.
        return session;
    }
}
