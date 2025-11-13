using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.In.CA;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests for packet factory and versioning functionality.
/// </summary>
public class PacketFactoryTests
{
    [Fact]
    public void Initialize_ScansAndRegistersPackets()
    {
        // Arrange
        var factory = new PacketFactory();
        
        // Act
        factory.Initialize();
        
        // Assert - Check that known packets are registered
        Assert.True(factory.CanCreatePacket(PacketHeader.CA_LOGIN));
        Assert.True(factory.CanCreatePacket(PacketHeader.CZ_HEARTBEAT));
    }
    
    [Fact]
    public void RegisterPacket_NewPacket_Succeeds()
    {
        // Arrange
        var factory = new PacketFactory();
        
        // Act
        factory.RegisterPacket<CA_LOGIN>(PacketHeader.CA_LOGIN, 1);
        
        // Assert
        Assert.True(factory.CanCreatePacket(PacketHeader.CA_LOGIN));
    }
    
    [Fact]
    public void RegisterPacket_DuplicateVersion_ThrowsException()
    {
        // Arrange
        var factory = new PacketFactory();
        factory.RegisterPacket<CA_LOGIN>(PacketHeader.CA_LOGIN, 1);
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            factory.RegisterPacket<CA_LOGIN>(PacketHeader.CA_LOGIN, 1));
    }
    
    [Fact]
    public void SetActiveVersion_RegisteredPacket_Succeeds()
    {
        // Arrange
        var factory = new PacketFactory();
        factory.RegisterPacket<CA_LOGIN>(PacketHeader.CA_LOGIN, 1);
        
        // Act
        factory.SetActiveVersion(PacketHeader.CA_LOGIN, 1);
        
        // Assert
        Assert.Equal(1, factory.GetActiveVersion(PacketHeader.CA_LOGIN));
    }
    
    [Fact]
    public void SetActiveVersion_UnregisteredVersion_ThrowsException()
    {
        // Arrange
        var factory = new PacketFactory();
        factory.RegisterPacket<CA_LOGIN>(PacketHeader.CA_LOGIN, 1);
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            factory.SetActiveVersion(PacketHeader.CA_LOGIN, 2));
    }
    
    [Fact]
    public void CreatePacket_RegisteredPacket_CreatesInstance()
    {
        // Arrange
        var factory = new PacketFactory();
        factory.RegisterPacket<CZ_HEARTBEAT>(PacketHeader.CZ_HEARTBEAT, 1);
        
        byte[] data = Array.Empty<byte>();
        
        // Act
        IncomingPacket packet;
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            packet = factory.CreatePacket(PacketHeader.CZ_HEARTBEAT, reader);
        }
        
        // Assert
        Assert.NotNull(packet);
        Assert.IsType<CZ_HEARTBEAT>(packet);
        Assert.Equal(PacketHeader.CZ_HEARTBEAT, packet.Header);
    }
    
    [Fact]
    public void CreatePacket_UnregisteredPacket_ThrowsException()
    {
        // Arrange
        var factory = new PacketFactory();
        byte[] data = Array.Empty<byte>();
        
        // Act & Assert
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            Assert.Throws<InvalidOperationException>(() =>
                factory.CreatePacket(PacketHeader.CA_LOGIN, reader));
        }
    }
    
    [Fact]
    public void CreatePacket_WithData_DeserializesCorrectly()
    {
        // Arrange
        var factory = new PacketFactory();
        factory.RegisterPacket<CA_LOGIN>(PacketHeader.CA_LOGIN, 1);
        
        // Create test data (body only, no header)
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((uint)1); // Version
            writer.WriteFixedString("TestUser", 24);
            writer.WriteFixedString("TestPass", 24);
            writer.Write((byte)5);
            data = ms.ToArray();
        }
        
        // Act
        CA_LOGIN packet;
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            packet = (CA_LOGIN)factory.CreatePacket(PacketHeader.CA_LOGIN, reader);
        }
        
        // Assert
        Assert.Equal("TestUser", packet.Username);
        Assert.Equal("TestPass", packet.Password);
        Assert.Equal(5, packet.Clienttype);
    }
    
    [Fact]
    public void GetActiveVersion_NoVersionSet_ReturnsHighestVersion()
    {
        // Arrange
        var factory = new PacketFactory();
        factory.RegisterPacket<CA_LOGIN>(PacketHeader.CA_LOGIN, 1);
        
        // Act
        var version = factory.GetActiveVersion(PacketHeader.CA_LOGIN);
        
        // Assert
        Assert.Equal(1, version);
    }
    
    [Fact]
    public void CanCreatePacket_UnregisteredPacket_ReturnsFalse()
    {
        // Arrange
        var factory = new PacketFactory();
        
        // Act
        bool canCreate = factory.CanCreatePacket(PacketHeader.CA_LOGIN);
        
        // Assert
        Assert.False(canCreate);
    }
}

