using Core.Server.Packets;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests for the packet size registry.
/// </summary>
public class PacketSizeRegistryTests
{
    [Fact]
    public void RegisterFixedSize_NewPacket_RegistersSuccessfully()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        var header = PacketHeader.AC_ACCEPT_LOGIN;
        int size = 7;
        
        // Act
        registry.RegisterFixedSize(header, size);
        
        // Assert
        Assert.True(registry.IsRegistered(header));
        Assert.True(registry.IsFixedLength(header));
        Assert.Equal(size, registry.GetFixedSize(header));
    }
    
    [Fact]
    public void RegisterVariableSize_NewPacket_RegistersSuccessfully()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        var header = PacketHeader.CA_LOGIN;
        
        // Act
        registry.RegisterVariableSize(header);
        
        // Assert
        Assert.True(registry.IsRegistered(header));
        Assert.False(registry.IsFixedLength(header));
        Assert.Null(registry.GetFixedSize(header));
    }
    
    [Fact]
    public void IsFixedLength_UnregisteredPacket_ThrowsException()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        var header = PacketHeader.CA_LOGIN;
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => registry.IsFixedLength(header));
    }
    
    [Fact]
    public void GetFixedSize_UnregisteredPacket_ReturnsNull()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        var header = PacketHeader.CA_LOGIN;
        
        // Act
        var size = registry.GetFixedSize(header);
        
        // Assert
        Assert.Null(size);
    }
    
    [Fact]
    public void GetFixedSize_VariableLengthPacket_ReturnsNull()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        var header = PacketHeader.CA_LOGIN;
        registry.RegisterVariableSize(header);
        
        // Act
        var size = registry.GetFixedSize(header);
        
        // Assert
        Assert.Null(size);
    }
    
    [Fact]
    public void Initialize_ScansAndRegistersPackets()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        
        // Act
        registry.Initialize();
        
        // Assert - Check that known packets are registered
        Assert.True(registry.IsRegistered(PacketHeader.CA_LOGIN));
        Assert.True(registry.IsRegistered(PacketHeader.CZ_HEARTBEAT));
        Assert.True(registry.IsRegistered(PacketHeader.AC_ACCEPT_LOGIN));
    }
    
    [Fact]
    public void Initialize_RegistersFixedLengthPacketsCorrectly()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        
        // Act
        registry.Initialize();
        
        // Assert - CZ_HEARTBEAT should be fixed-length with size 2
        Assert.True(registry.IsFixedLength(PacketHeader.CZ_HEARTBEAT));
        Assert.Equal(2, registry.GetFixedSize(PacketHeader.CZ_HEARTBEAT));
        
        // Assert - AC_ACCEPT_LOGIN should be fixed-length with size 7
        Assert.True(registry.IsFixedLength(PacketHeader.AC_ACCEPT_LOGIN));
        Assert.Equal(7, registry.GetFixedSize(PacketHeader.AC_ACCEPT_LOGIN));
    }
    
    [Fact]
    public void Initialize_RegistersVariableLengthPacketsCorrectly()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        
        // Act
        registry.Initialize();
        
        // Assert - CA_LOGIN should be variable-length
        Assert.False(registry.IsFixedLength(PacketHeader.CA_LOGIN));
        Assert.Null(registry.GetFixedSize(PacketHeader.CA_LOGIN));
        
        // Assert - HC_CHARACTER_LIST should be variable-length
        Assert.False(registry.IsFixedLength(PacketHeader.HC_CHARACTER_LIST));
        Assert.Null(registry.GetFixedSize(PacketHeader.HC_CHARACTER_LIST));
    }
    
    [Fact]
    public void RegisterFixedSize_InvalidSize_ThrowsException()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        
        // Act & Assert - Too small
        Assert.Throws<InvalidDataException>(() =>
            registry.RegisterFixedSize(PacketHeader.AC_ACCEPT_LOGIN, 1));
        
        // Act & Assert - Too large
        Assert.Throws<InvalidDataException>(() =>
            registry.RegisterFixedSize(PacketHeader.AC_ACCEPT_LOGIN, 40000));
    }
    
    [Fact]
    public void IsRegistered_RegisteredPacket_ReturnsTrue()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        registry.RegisterFixedSize(PacketHeader.AC_ACCEPT_LOGIN, 7);
        
        // Act & Assert
        Assert.True(registry.IsRegistered(PacketHeader.AC_ACCEPT_LOGIN));
    }
    
    [Fact]
    public void IsRegistered_UnregisteredPacket_ReturnsFalse()
    {
        // Arrange
        var registry = new PacketSizeRegistry();
        
        // Act & Assert
        Assert.False(registry.IsRegistered(PacketHeader.AC_ACCEPT_LOGIN));
    }
}

