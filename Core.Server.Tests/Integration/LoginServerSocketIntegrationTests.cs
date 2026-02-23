using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Core.Server;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Timer;
using Login.Server;
using Login.Server.Handlers;
using Login.Server.Model;
using Login.Server.Repository.Api;
using Login.Server.Repository.Impl;
using Login.Server.Security;
using Login.Server.UseCase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Server.Tests.Integration;

public class LoginServerSocketIntegrationTests
{
    [Fact]
    public async Task LoginServer_WhenHashRequestPacketArrives_SendsAckHashResponse()
    {
        var tcpPort = GetFreeTcpPort();
        var config = new LoginServerConfiguration
        {
            Port = tcpPort,
            GrpcPort = GetFreeTcpPort(),
            TargetFPS = 20,
            HeartbeatTimeout = 30000,
            MaxConnections = 1000,
            IpBan = false,
            OtherServerEndpoints = new Dictionary<string, string>()
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<LoginServerSocketIntegrationTests>>());
        services.AddSingleton<ServerConfiguration>(config);
        services.AddSingleton(config);
        services.AddSingleton<PacketSystem>(_ =>
        {
            var packetSystem = new PacketSystem();
            packetSystem.Initialize();
            return packetSystem;
        });
        services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
        services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);
        services.AddSingleton<SessionManager>();
        services.AddSingleton<ILoginSecurityService, TestLoginSecurityService>();
        services.AddSingleton<ILoginDataRepository, TestLoginDataRepository>();
        services.AddSingleton<ILoginMmoAuth, TestLoginMmoAuth>();
        services.AddSingleton<ILoginAuthUseCase, LoginAuthUseCase>();
        services.AddSingleton<ILoginSessionPacketUseCase, LoginSessionPacketUseCase>();
        services.AddSingleton<LoginServerImpl>();

        var handlerTypes = typeof(LoginServerImpl).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() != null);

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }

        await using var provider = services.BuildServiceProvider();
        var server = provider.GetRequiredService<LoginServerImpl>();
        var reqHashHandler = provider.GetRequiredService<ReqHashHandler>();
        var loginHandler = provider.GetRequiredService<LoginHandler>();

        // Ensure the CA_REQ_HASH path is wired in this in-process integration setup.
        var registryField = typeof(LoginServerImpl).GetField("_handlerRegistry", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not access LoginServerImpl handler registry.");
        var registry = registryField.GetValue(server) as PacketHandlerRegistry
            ?? throw new InvalidOperationException("Handler registry is not initialized.");
        registry.RegisterHandler(PacketHeader.CA_REQ_HASH, reqHashHandler);
        registry.RegisterHandler(PacketHeader.CA_LOGIN, loginHandler);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await server.StartAsync(cts.Token);

        try
        {
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(IPAddress.Loopback, tcpPort, cts.Token);

            var request = BuildReqHashPacket();
            await client.SendAsync(request, SocketFlags.None, cts.Token);

            var responseHeaderBytes = await ReadExactAsync(client, 2, cts.Token);
            var responseSizeBytes = await ReadExactAsync(client, 2, cts.Token);

            var header = BitConverter.ToInt16(responseHeaderBytes, 0);
            var packetLength = BitConverter.ToInt16(responseSizeBytes, 0);

            Assert.Equal((short)PacketHeader.AC_ACK_HASH, header);
            Assert.Equal(20, packetLength);

            var saltBytes = await ReadExactAsync(client, packetLength - 4, cts.Token);
            Assert.Equal(16, saltBytes.Length);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task LoginServer_WhenLoginPacketArrives_SendsRefuseLoginResponse()
    {
        var tcpPort = GetFreeTcpPort();
        var config = new LoginServerConfiguration
        {
            Port = tcpPort,
            GrpcPort = GetFreeTcpPort(),
            TargetFPS = 20,
            HeartbeatTimeout = 30000,
            MaxConnections = 1000,
            IpBan = false,
            OtherServerEndpoints = new Dictionary<string, string>()
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<LoginServerSocketIntegrationTests>>());
        services.AddSingleton<ServerConfiguration>(config);
        services.AddSingleton(config);
        services.AddSingleton<PacketSystem>(_ =>
        {
            var packetSystem = new PacketSystem();
            packetSystem.Initialize();
            return packetSystem;
        });
        services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
        services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);
        services.AddSingleton<SessionManager>();
        services.AddSingleton<ILoginSecurityService, TestLoginSecurityService>();
        services.AddSingleton<ILoginDataRepository, TestLoginDataRepository>();
        services.AddSingleton<ILoginMmoAuth, TestLoginMmoAuth>();
        services.AddSingleton<ILoginAuthUseCase, LoginAuthUseCase>();
        services.AddSingleton<ILoginSessionPacketUseCase, LoginSessionPacketUseCase>();
        services.AddSingleton<LoginServerImpl>();

        var handlerTypes = typeof(LoginServerImpl).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() != null);

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }

        await using var provider = services.BuildServiceProvider();
        var server = provider.GetRequiredService<LoginServerImpl>();
        var loginHandler = provider.GetRequiredService<LoginHandler>();

        var registryField = typeof(LoginServerImpl).GetField("_handlerRegistry", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not access LoginServerImpl handler registry.");
        var registry = registryField.GetValue(server) as PacketHandlerRegistry
            ?? throw new InvalidOperationException("Handler registry is not initialized.");
        registry.RegisterHandler(PacketHeader.CA_LOGIN, loginHandler);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await server.StartAsync(cts.Token);

        try
        {
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(IPAddress.Loopback, tcpPort, cts.Token);

            var request = BuildLoginPacket("test_user", "wrong_pass", clientType: 0);
            await client.SendAsync(request, SocketFlags.None, cts.Token);

            var responseHeaderBytes = await ReadExactAsync(client, 2, cts.Token);
            var responseBodyBytes = await ReadExactAsync(client, 24, cts.Token);

            var header = BitConverter.ToInt16(responseHeaderBytes, 0);
            var errorCode = BitConverter.ToUInt32(responseBodyBytes, 0);

            Assert.Equal((short)PacketHeader.AC_REFUSE_LOGIN, header);
            Assert.Equal<uint>(0, errorCode);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task LoginServer_WhenCtAuthPacketArrives_SendsTcResultResponse()
    {
        var tcpPort = GetFreeTcpPort();
        var config = new LoginServerConfiguration
        {
            Port = tcpPort,
            GrpcPort = GetFreeTcpPort(),
            TargetFPS = 20,
            HeartbeatTimeout = 30000,
            MaxConnections = 1000,
            IpBan = false,
            OtherServerEndpoints = new Dictionary<string, string>()
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<LoginServerSocketIntegrationTests>>());
        services.AddSingleton<ServerConfiguration>(config);
        services.AddSingleton(config);
        services.AddSingleton<PacketSystem>(_ =>
        {
            var packetSystem = new PacketSystem();
            packetSystem.Initialize();
            return packetSystem;
        });
        services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
        services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);
        services.AddSingleton<SessionManager>();
        services.AddSingleton<ILoginSecurityService, TestLoginSecurityService>();
        services.AddSingleton<ILoginDataRepository, TestLoginDataRepository>();
        services.AddSingleton<ILoginMmoAuth, TestLoginMmoAuth>();
        services.AddSingleton<ILoginAuthUseCase, LoginAuthUseCase>();
        services.AddSingleton<ILoginSessionPacketUseCase, LoginSessionPacketUseCase>();
        services.AddSingleton<LoginServerImpl>();

        var handlerTypes = typeof(LoginServerImpl).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() != null);

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }

        await using var provider = services.BuildServiceProvider();
        var server = provider.GetRequiredService<LoginServerImpl>();
        var otpAuthHandler = provider.GetRequiredService<OtpAuthHandler>();

        var registryField = typeof(LoginServerImpl).GetField("_handlerRegistry", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not access LoginServerImpl handler registry.");
        var registry = registryField.GetValue(server) as PacketHandlerRegistry
            ?? throw new InvalidOperationException("Handler registry is not initialized.");
        registry.RegisterHandler(PacketHeader.CT_AUTH, otpAuthHandler);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await server.StartAsync(cts.Token);

        try
        {
            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(IPAddress.Loopback, tcpPort, cts.Token);

            var request = BuildCtAuthPacket();
            await client.SendAsync(request, SocketFlags.None, cts.Token);

            var headerBytes = await ReadExactAsync(client, 2, cts.Token);
            var lengthBytes = await ReadExactAsync(client, 2, cts.Token);
            var typeBytes = await ReadExactAsync(client, 4, cts.Token);

            var header = BitConverter.ToInt16(headerBytes, 0);
            var packetLength = BitConverter.ToInt16(lengthBytes, 0);
            var resultType = BitConverter.ToUInt32(typeBytes, 0);

            Assert.Equal((short)PacketHeader.TC_RESULT, header);
            Assert.Equal(34, packetLength);
            Assert.Equal<uint>(0, resultType);
        }
        finally
        {
            await server.StopAsync(CancellationToken.None);
        }
    }

    private static byte[] BuildReqHashPacket()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)PacketHeader.CA_REQ_HASH);
        return ms.ToArray();
    }

    private static byte[] BuildLoginPacket(string username, string password, byte clientType)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)PacketHeader.CA_LOGIN);
        writer.Write((uint)1);
        writer.WriteFixedString(username, 24);
        writer.WriteFixedString(password, 24);
        writer.Write(clientType);
        return ms.ToArray();
    }

    private static byte[] BuildCtAuthPacket()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)PacketHeader.CT_AUTH);
        writer.Write(new byte[66]);
        return ms.ToArray();
    }

    private static async Task<byte[]> ReadExactAsync(Socket socket, int length, CancellationToken cancellationToken)
    {
        var buffer = new byte[length];
        var read = 0;

        while (read < length)
        {
            var received = await socket.ReceiveAsync(buffer.AsMemory(read, length - read), SocketFlags.None, cancellationToken);
            if (received <= 0)
            {
                throw new IOException($"Socket closed before reading {length} bytes (read {read}).");
            }

            read += received;
        }

        return buffer;
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class TestLoginMmoAuth : ILoginMmoAuth
    {
        public Task<ILoginMmoAuth.Output> ExecuteAsync(ILoginMmoAuth.Input input)
            => Task.FromResult(new ILoginMmoAuth.Output(0));
    }

    private sealed class TestLoginSecurityService : ILoginSecurityService
    {
        public Task<bool> IsIpBannedAsync(IPAddress ip, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task LogLoginAttemptAsync(IPAddress ip, string username, int resultCode, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnforceDynamicPasswordFailureBanAsync(IPAddress ip, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CleanupExpiredIpBansAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestLoginDataRepository : ILoginDataRepository
    {
        private readonly Dictionary<int, OnlineLoginData> _onlineUsers = new();
        private readonly Dictionary<int, AuthNode> _authNodes = new();

        public OnlineLoginData? GetOnlineUser(int accountId)
            => _onlineUsers.TryGetValue(accountId, out var data) ? data : null;

        public OnlineLoginData AddOnlineUser(int charServer, int accountId)
        {
            var data = new OnlineLoginData(
                new AccountId(accountId),
                charServer,
                Scheduler.InvalidTimer,
                Scheduler.InvalidTimer);
            _onlineUsers[accountId] = data;
            return data;
        }

        public void RemoveOnlineUser(int accountId) => _onlineUsers.Remove(accountId);

        public int RemoveOnlineUsersByCharServer(int charServer)
        {
            var toRemove = _onlineUsers
                .Where(kv => kv.Value.CharServer == charServer)
                .Select(kv => kv.Key)
                .ToArray();
            foreach (var accountId in toRemove)
            {
                _onlineUsers.Remove(accountId);
            }

            return toRemove.Length;
        }

        public void SetOnlineUserCharServer(int accountId, int charServer)
        {
            if (_onlineUsers.TryGetValue(accountId, out var current))
            {
                _onlineUsers[accountId] = current with { CharServer = charServer };
            }
        }

        public void Update(OnlineLoginData onlineLoginData)
            => _onlineUsers[onlineLoginData.AccountId.Value] = onlineLoginData;

        public AuthNode? GetAuthNode(int accountId)
            => _authNodes.TryGetValue(accountId, out var node) ? node : null;

        public AuthNode AddAuthNode(LoginSessionData sd)
        {
            var node = new AuthNode(sd.AccountId, sd.LoginId1, sd.LoginId2, 0, sd.Sex, sd.ClientType);
            _authNodes[sd.AccountId] = node;
            return node;
        }

        public bool TryConsumeAuthNode(int accountId, int loginId1, int loginId2, char sex, out AuthNode? authNode)
        {
            if (_authNodes.TryGetValue(accountId, out var node) &&
                node.LoginId1 == loginId1 &&
                node.LoginId2 == loginId2 &&
                node.Sex == sex)
            {
                authNode = node;
                _authNodes.Remove(accountId);
                return true;
            }

            authNode = null;
            return false;
        }

        public void RemoveAuthNode(int accountId) => _authNodes.Remove(accountId);
    }
}
