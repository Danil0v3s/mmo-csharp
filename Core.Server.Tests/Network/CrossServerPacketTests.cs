using System.Net.Sockets;
using System.Reflection;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Core.Server.Tests.Network;

/// <summary>
/// Tests to validate that servers only handle their designated packets
/// and disconnect clients that send packets meant for other servers.
/// </summary>
public class CrossServerPacketTests
{
    [Fact]
    public async Task LoginServer_ShouldHandleLoginPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateLoginServerContext();
        var packet = new CA_LOGIN { Username = "user", Password = "pass" };

        // Act
        session.IncomingPackets.Enqueue(packet);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets, 
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.True(session.IsAlive, "Should remain connected for valid login packet");
    }

    [Fact]
    public async Task LoginServer_ShouldDisconnectForCharServerPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateLoginServerContext();
        var charPacket = new MockIncomingPacket(PacketHeader.CH_CHARLIST_REQ);

        // Act - Send char server packet to login server
        session.IncomingPackets.Enqueue(charPacket);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.False(session.IsAlive, "Should disconnect client sending char server packet to login server");
    }

    [Fact]
    public async Task LoginServer_ShouldDisconnectForMapServerPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateLoginServerContext();
        var mapPacket = new MockIncomingPacket(PacketHeader.CZ_ENTER);

        // Act - Send map server packet to login server
        session.IncomingPackets.Enqueue(mapPacket);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.False(session.IsAlive, "Should disconnect client sending map server packet to login server");
    }

    [Fact]
    public async Task CharServer_ShouldHandleCharPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateCharServerContext();
        
        // Verify the handler is registered
        Assert.True(registry.HasHandler(PacketHeader.CH_CHARLIST_REQ), 
            "CharServer should have handler for CH_CHARLIST_REQ");
        
        var packet = new MockIncomingPacket(PacketHeader.CH_CHARLIST_REQ);

        // Act
        session.IncomingPackets.Enqueue(packet);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.True(session.IsAlive, "Should remain connected for valid char packet");
    }

    [Fact]
    public async Task CharServer_ShouldDisconnectForLoginServerPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateCharServerContext();
        var loginPacket = new MockIncomingPacket(PacketHeader.CA_LOGIN);

        // Act - Send login server packet to char server
        session.IncomingPackets.Enqueue(loginPacket);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.False(session.IsAlive, "Should disconnect client sending login server packet to char server");
    }

    [Fact]
    public async Task CharServer_ShouldDisconnectForMapServerPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateCharServerContext();
        var mapPacket = new MockIncomingPacket(PacketHeader.CZ_ENTER);

        // Act - Send map server packet to char server
        session.IncomingPackets.Enqueue(mapPacket);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.False(session.IsAlive, "Should disconnect client sending map server packet to char server");
    }

    [Fact]
    public async Task MapServer_ShouldHandleMapPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateMapServerContext();
        
        // Verify the handler is registered
        Assert.True(registry.HasHandler(PacketHeader.CZ_ENTER), 
            "MapServer should have handler for CZ_ENTER");
        
        var packet = new MockIncomingPacket(PacketHeader.CZ_ENTER);

        // Act
        session.IncomingPackets.Enqueue(packet);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.True(session.IsAlive, "Should remain connected for valid map packet");
    }

    [Fact]
    public async Task MapServer_ShouldDisconnectForLoginServerPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateMapServerContext();
        var loginPacket = new MockIncomingPacket(PacketHeader.CA_LOGIN);

        // Act - Send login server packet to map server
        session.IncomingPackets.Enqueue(loginPacket);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.False(session.IsAlive, "Should disconnect client sending login server packet to map server");
    }

    [Fact]
    public async Task MapServer_ShouldDisconnectForCharServerPackets()
    {
        // Arrange
        var (registry, session, logger) = CreateMapServerContext();
        var charPacket = new MockIncomingPacket(PacketHeader.CH_CHARLIST_REQ);

        // Act - Send char server packet to map server
        session.IncomingPackets.Enqueue(charPacket);
        await registry.ProcessMockSessionPacketsAsync(session, session.IncomingPackets,
            reason => session.Disconnect(reason), logger);

        // Assert
        Assert.False(session.IsAlive, "Should disconnect client sending char server packet to map server");
    }

    [Fact]
    public void LoginServer_ShouldOnlyRegisterLoginPacketHandlers()
    {
        // Arrange
        var (registry, _, _) = CreateLoginServerContext();

        // Assert - Check that ONLY login packets are registered
        Assert.True(registry.HasHandler(PacketHeader.CA_LOGIN), 
            "Login server should handle CA_LOGIN");
        
        // In real implementation, more handlers would be registered
        // For this test, we only registered CA_LOGIN to demonstrate isolation
    }

    [Fact]
    public void LoginServer_ShouldNotRegisterOtherServerPackets()
    {
        // Arrange
        var (registry, _, _) = CreateLoginServerContext();

        // Assert - Char server packets
        Assert.False(registry.HasHandler(PacketHeader.CH_CHARLIST_REQ), 
            "Login server should NOT handle char server packets");
        Assert.False(registry.HasHandler(PacketHeader.CH_MAKE_CHAR), 
            "Login server should NOT handle char server packets");

        // Assert - Map server packets
        Assert.False(registry.HasHandler(PacketHeader.CZ_ENTER), 
            "Login server should NOT handle map server packets");
        Assert.False(registry.HasHandler(PacketHeader.CZ_REQUEST_MOVE), 
            "Login server should NOT handle map server packets");
    }

    // Helper methods to create server contexts
    private static (PacketHandlerRegistry registry, MockClientSession session, ILogger logger) CreateLoginServerContext()
    {
        var logger = new TestLogger<CrossServerPacketTests>();
        var services = new ServiceCollection()
            .AddSingleton<ILogger>(logger)
            .AddTransient<MockLoginHandler>()
            .BuildServiceProvider();

        var registry = new PacketHandlerRegistry(services, logger);
        // Manually register only LOGIN handlers (not all handlers in assembly)
        registry.RegisterHandler(PacketHeader.CA_LOGIN, new MockLoginHandler());

        var session = new MockClientSession(logger);
        return (registry, session, logger);
    }

    private static (PacketHandlerRegistry registry, MockClientSession session, ILogger logger) CreateCharServerContext()
    {
        var logger = new TestLogger<CrossServerPacketTests>();
        var services = new ServiceCollection()
            .AddSingleton<ILogger>(logger)
            .AddTransient<MockCharHandler>()
            .BuildServiceProvider();

        var registry = new PacketHandlerRegistry(services, logger);
        // Manually register only CHAR handlers (not all handlers in assembly)
        registry.RegisterHandler(PacketHeader.CH_CHARLIST_REQ, new MockCharHandler());

        var session = new MockClientSession(logger);
        return (registry, session, logger);
    }

    private static (PacketHandlerRegistry registry, MockClientSession session, ILogger logger) CreateMapServerContext()
    {
        var logger = new TestLogger<CrossServerPacketTests>();
        var services = new ServiceCollection()
            .AddSingleton<ILogger>(logger)
            .AddTransient<MockMapHandler>()
            .BuildServiceProvider();

        var registry = new PacketHandlerRegistry(services, logger);
        // Manually register only MAP handlers (not all handlers in assembly)
        registry.RegisterHandler(PacketHeader.CZ_ENTER, new MockMapHandler());

        var session = new MockClientSession(logger);
        return (registry, session, logger);
    }

    // Helper to create packet with specific header (using reflection since Header is read-only)
    private static T CreatePacketWithHeader<T>(PacketHeader header) where T : IncomingPacket, new()
    {
        var packet = new T();
        var headerProperty = typeof(T).BaseType?.GetProperty("Header");
        if (headerProperty != null)
        {
            var backingField = typeof(T).BaseType?.GetField("<Header>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            backingField?.SetValue(packet, header);
        }
        return packet;
    }

    // Mock handlers for each server type
    [PacketHandler(PacketHeader.CA_LOGIN)]
    public class MockLoginHandler : IPacketHandler<CA_LOGIN>
    {
        public Task HandleAsync(ClientSession session, CA_LOGIN packet)
        {
            return Task.CompletedTask;
        }
    }

    [PacketHandler(PacketHeader.CH_CHARLIST_REQ)]
    public class MockCharHandler : IPacketHandler<CZ_HEARTBEAT>
    {
        public Task HandleAsync(ClientSession session, CZ_HEARTBEAT packet)
        {
            return Task.CompletedTask;
        }
    }

    [PacketHandler(PacketHeader.CZ_ENTER)]
    public class MockMapHandler : IPacketHandler<CZ_HEARTBEAT>
    {
        public Task HandleAsync(ClientSession session, CZ_HEARTBEAT packet)
        {
            return Task.CompletedTask;
        }
    }
}

