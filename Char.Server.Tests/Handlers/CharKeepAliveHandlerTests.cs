using System.Net;
using System.Net.Sockets;
using Char.Server;
using Char.Server.Handlers;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Handlers;

public class CharKeepAliveHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSessionUnbound_ShouldDisconnect()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"));

        var handler = new CharKeepAliveHandler(loggerFactory.CreateLogger<CharKeepAliveHandler>());

        await handler.HandleAsync(session, BuildPacket(2000000));

        Assert.False(session.IsAlive);
    }

    [Fact]
    public async Task HandleAsync_WhenAccountMismatch_ShouldDisconnect()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"))
        {
            AccountId = 2000000
        };

        var handler = new CharKeepAliveHandler(loggerFactory.CreateLogger<CharKeepAliveHandler>());

        await handler.HandleAsync(session, BuildPacket(2000001));

        Assert.False(session.IsAlive);
    }

    [Fact]
    public async Task HandleAsync_WhenAccountMatches_ShouldKeepSessionAlive()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"))
        {
            AccountId = 2000000
        };

        var handler = new CharKeepAliveHandler(loggerFactory.CreateLogger<CharKeepAliveHandler>());

        await handler.HandleAsync(session, BuildPacket(2000000));

        Assert.True(session.IsAlive);
    }

    private static CH_KEEP_ALIVE BuildPacket(uint accountId)
    {
        var packet = new CH_KEEP_ALIVE();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(accountId);
        }

        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        packet.Read(reader);
        return packet;
    }

    private static SocketPair CreateSocketPair()
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

    private sealed record SocketPair(Socket ServerSide, Socket ClientSide) : IDisposable
    {
        public void Dispose()
        {
            try { ServerSide.Close(); } catch { }
            try { ClientSide.Close(); } catch { }
            ServerSide.Dispose();
            ClientSide.Dispose();
        }
    }
}
