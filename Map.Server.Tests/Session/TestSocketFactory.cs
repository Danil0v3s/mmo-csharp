using System.Net;
using System.Net.Sockets;

namespace Map.Server.Tests.Session;

/// <summary>
/// Loopback socket pair helper shared by every handler test that needs a
/// real <see cref="Socket"/> to hand to <c>ClientSession</c>'s constructor.
/// </summary>
internal static class TestSocketFactory
{
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

        return new SocketPair(server, client);
    }
}
