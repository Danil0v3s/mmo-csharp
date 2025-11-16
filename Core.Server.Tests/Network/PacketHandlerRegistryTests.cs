using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.In.CA;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Server.Tests.Network;

public class PacketHandlerRegistryTests
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public PacketHandlerRegistryTests()
    {
        // Reset static state before each test
        TestLoginHandler.Reset();
        TestCharHandler.Reset();
        TestMapHandler.Reset();
        
        var services = new ServiceCollection()
            .AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<PacketHandlerRegistryTests>>())
            .AddSingleton<ILogger<PacketHandlerRegistryTests>>(new TestLogger<PacketHandlerRegistryTests>())
            .AddTransient<TestLoginHandler>()
            .AddTransient<TestCharHandler>()
            .AddTransient<TestMapHandler>()
            .BuildServiceProvider();

        _serviceProvider = services;
        _logger = services.GetRequiredService<ILogger>();
    }

    [Fact]
    public void Registry_ShouldDiscoverHandlersFromAssembly()
    {
        // Arrange
        var registry = new PacketHandlerRegistry(_serviceProvider, _logger);

        // Act
        registry.DiscoverAndRegister(Assembly.GetExecutingAssembly());

        // Assert - The assembly now contains both Test and Mock handlers, 
        // but only one handler per packet type is registered (duplicates are rejected)
        Assert.True(registry.HasHandler(PacketHeader.CA_LOGIN), "Should find CA_LOGIN handler");
        Assert.True(registry.HasHandler(PacketHeader.CH_CHARLIST_REQ), "Should find CH_CHARLIST_REQ handler");
        Assert.True(registry.HasHandler(PacketHeader.CZ_ENTER), "Should find CZ_ENTER handler");
        Assert.Equal(3, registry.HandlerCount);
    }

    [Fact]
    public async Task Registry_ShouldHandleCorrectPacketWithHandler()
    {
        // Arrange
        var registry = new PacketHandlerRegistry(_serviceProvider, _logger);
        // Manually register to avoid conflicts with Mock handlers
        registry.RegisterHandler(PacketHeader.CA_LOGIN, new TestLoginHandler());
        
        var session = CreateTestSession();
        var packet = new CA_LOGIN { Username = "test", Password = "test" };

        // Act
        var handled = await registry.TryHandlePacketAsync(session, packet);

        // Assert
        Assert.True(handled, "Packet should be handled");
        Assert.True(TestLoginHandler.WasCalled, "Handler should have been called");
    }

    [Fact]
    public async Task Registry_ShouldReturnFalseForUnhandledPacket()
    {
        // Arrange
        var registry = new PacketHandlerRegistry(_serviceProvider, _logger);
        registry.DiscoverAndRegister(Assembly.GetExecutingAssembly());
        
        var session = CreateTestSession();
        var packet = new CZ_HEARTBEAT(); // Not registered

        // Act
        var handled = await registry.TryHandlePacketAsync(session, packet);

        // Assert
        Assert.False(handled, "Unknown packet should not be handled");
    }

    [Fact]
    public async Task Registry_ShouldInvokeHandlerWithCorrectParameters()
    {
        // Arrange
        var registry = new PacketHandlerRegistry(_serviceProvider, _logger);
        // Manually register to avoid conflicts with Mock handlers
        registry.RegisterHandler(PacketHeader.CA_LOGIN, new TestLoginHandler());
        
        var session = CreateTestSession();
        var packet = new CA_LOGIN { Username = "testuser", Password = "pass123" };

        // Act
        await registry.TryHandlePacketAsync(session, packet);

        // Assert
        Assert.NotNull(TestLoginHandler.LastSession);
        Assert.NotNull(TestLoginHandler.LastPacket);
        Assert.Equal("testuser", TestLoginHandler.LastPacket!.Username);
        Assert.Equal(session.SessionId, TestLoginHandler.LastSession!.SessionId);
    }

    [Fact]
    public void Registry_ShouldNotRegisterDuplicateHandlers()
    {
        // Arrange
        var registry = new PacketHandlerRegistry(_serviceProvider, _logger);

        // Act - Register twice
        registry.DiscoverAndRegister(Assembly.GetExecutingAssembly());
        registry.DiscoverAndRegister(Assembly.GetExecutingAssembly());

        // Assert - Should still only have 3 handlers (duplicates ignored)
        Assert.Equal(3, registry.HandlerCount);
    }

    private static ClientSession CreateTestSession()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var packetSystem = new PacketSystem();
        packetSystem.Initialize();
        var logger = new TestLogger<ClientSession>();
        return new ClientSession(socket, 30000, packetSystem.Factory, packetSystem.Registry, logger);
    }

    // Test handlers
    [PacketHandler(PacketHeader.CA_LOGIN)]
    public class TestLoginHandler : IPacketHandler<CA_LOGIN>
    {
        public static bool WasCalled { get; private set; }
        public static ClientSession? LastSession { get; private set; }
        public static CA_LOGIN? LastPacket { get; private set; }

        public static void Reset()
        {
            WasCalled = false;
            LastSession = null;
            LastPacket = null;
        }

        public Task HandleAsync(ClientSession session, CA_LOGIN packet)
        {
            WasCalled = true;
            LastSession = session;
            LastPacket = packet;
            return Task.CompletedTask;
        }
    }

    [PacketHandler(PacketHeader.CH_CHARLIST_REQ)]
    public class TestCharHandler : IPacketHandler<CZ_HEARTBEAT>
    {
        public static bool WasCalled { get; private set; }

        public static void Reset()
        {
            WasCalled = false;
        }

        public Task HandleAsync(ClientSession session, CZ_HEARTBEAT packet)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    [PacketHandler(PacketHeader.CZ_ENTER)]
    public class TestMapHandler : IPacketHandler<CZ_HEARTBEAT>
    {
        public static bool WasCalled { get; private set; }

        public static void Reset()
        {
            WasCalled = false;
        }

        public Task HandleAsync(ClientSession session, CZ_HEARTBEAT packet)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }
}

// Simple test logger implementation
public class TestLogger<T> : ILogger<T>
{
    private readonly ConcurrentBag<string> _logs = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logs.Add(formatter(state, exception));
    }

    public IReadOnlyCollection<string> Logs => _logs;
}

