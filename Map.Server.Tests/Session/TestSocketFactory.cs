using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Map.Server.Tests.Session;

/// <summary>
/// Loopback socket pair helper shared by every handler test that needs a
/// real <see cref="Socket"/> to hand to <c>ClientSession</c>'s constructor.
///
/// <para>
/// <b>Lifetime note.</b> Tests typically capture only the <see cref="SocketPair.ServerSide"/>
/// on a <c>MapSessionData</c> and let the local <c>SocketPair</c> reference
/// go out of scope. Under parallel test load the unreferenced
/// <see cref="SocketPair.ClientSide"/> socket gets finalized — that closes
/// its half of the loopback connection, which makes the server side's
/// <c>ReceiveLoopAsync</c> read 0 bytes and call <c>Disconnect()</c>, flipping
/// <c>ClientSession.IsAlive</c> to <c>false</c>. Subsequent
/// <c>EnqueuePacket</c> calls silently no-op, so a test asserting on the
/// outbound queue sees an empty collection and fails with no other hint —
/// exactly the trade / storage flakes that surfaced under `--filter ~Trade`.
/// </para>
///
/// <para>
/// The fix: every pair is rooted in <see cref="_keepAlive"/> for the test
/// process's lifetime. FDs are reclaimed at process exit; for short-lived
/// xUnit runs this is fine and is the smallest fix that doesn't force every
/// test harness to thread a disposable through its <c>AddPc</c> helpers.
/// </para>
/// </summary>
internal static class TestSocketFactory
{
    // Rooted forever so the loopback peer doesn't get finalized mid-test.
    // Concurrent because xUnit calls into here from parallel test classes.
    private static readonly ConcurrentBag<SocketPair> _keepAlive = new();

    internal sealed record SocketPair(Socket ServerSide, Socket ClientSide) : IDisposable
    {
        public void Dispose()
        {
            try { ServerSide.Close(); } catch { }
            try { ClientSide.Close(); } catch { }
            ServerSide.Dispose();
            ClientSide.Dispose();
        }
    }

    internal static SocketPair CreateSocketPair()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;

        var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(endpoint);

        var server = listener.AcceptSocket();
        listener.Stop();

        var pair = new SocketPair(server, client);
        _keepAlive.Add(pair);
        return pair;
    }
}
