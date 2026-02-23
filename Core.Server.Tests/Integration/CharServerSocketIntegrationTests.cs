using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Char.Server;
using Core.Server;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Server.Tests.Integration;

public class CharServerSocketIntegrationTests
{
    [Fact]
    public async Task CharServer_WhenKeepAlivePacketArrives_ProcessesHandlerViaSocket()
    {
        var tcpPort = GetFreeTcpPort();

        var configuration = new CharServerConfiguration
        {
            Port = tcpPort,
            GrpcPort = GetFreeTcpPort(),
            TargetFPS = 20,
            HeartbeatTimeout = 30000,
            MaxConnections = 1000,
            OtherServerEndpoints = new Dictionary<string, string>()
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ServerConfiguration>(configuration);
        services.AddSingleton(configuration);
        services.AddSingleton<SessionManager>();

        var handlerTypes = typeof(CharServerImpl).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() != null);

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<CharServerImpl>>();
        var server = new CharServerImpl(configuration, logger, provider);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await server.StartAsync(cts.Token);

        try
        {
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(IPAddress.Loopback, tcpPort, cts.Token);

            const uint accountId = 123456;
            var payload = BuildKeepAlivePacket(accountId);
            await client.SendAsync(payload, SocketFlags.None, cts.Token);

            // Wait until game loop processes the packet and the handler binds account to session.
            var disconnected = 0;
            for (var i = 0; i < 40; i++)
            {
                disconnected = await server.ForceDisconnectAccountAsync((int)accountId);
                if (disconnected > 0)
                {
                    break;
                }

                await Task.Delay(50, cts.Token);
            }

            Assert.Equal(1, disconnected);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
        }
    }

    private static byte[] BuildKeepAlivePacket(uint accountId)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)PacketHeader.CH_KEEP_ALIVE);
        writer.Write(accountId);
        return ms.ToArray();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
