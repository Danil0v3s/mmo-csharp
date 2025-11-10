using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Xunit;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests for packet size calculation accuracy.
/// </summary>
public class PacketSizeTests
{
    [Fact]
    public void FixedLengthPacket_GetSize_ReturnsCorrectSize()
    {
        // Arrange
        var packet = new AC_ACCEPT_LOGIN
        {
            SessionToken = 123,
            CharacterSlots = 5
        };
        
        // Act
        int size = packet.GetSize();
        
        // Assert - Header (2) + SessionToken (4) + CharacterSlots (1) = 7
        Assert.Equal(7, size);
        Assert.True(packet.IsFixedLength);
    }
    
    [Fact]
    public void HeartbeatPacket_GetSize_ReturnsHeaderOnly()
    {
        // Arrange
        var packet = new CZ_HEARTBEAT();
        
        // Act
        int size = packet.GetSize();
        
        // Assert - Only header (2)
        Assert.Equal(2, size);
        Assert.True(packet.IsFixedLength);
    }
    
    [Fact]
    public void VariableLengthPacket_GetSize_IncludesSizeField()
    {
        // Arrange
        var packet = new CA_LOGIN
        {
            Username = "test",
            Password = "pass",
            ClientType = 1
        };
        
        // Act
        int size = packet.GetSize();
        
        // Assert - Header (2) + Size field (2) + Username (24) + Password (24) + ClientType (1) = 53
        Assert.Equal(53, size);
        Assert.False(packet.IsFixedLength);
    }
    
    [Fact]
    public void PacketWithEmptyArray_GetSize_ReturnsMinimumSize()
    {
        // Arrange
        var packet = new HC_CHARACTER_LIST
        {
            Characters = Array.Empty<CharacterInfo>()
        };
        
        // Act
        int size = packet.GetSize();
        
        // Assert - Header (2) + Size (2) + Count (1) + 0 characters = 5
        Assert.Equal(5, size);
        Assert.False(packet.IsFixedLength);
    }
    
    [Fact]
    public void PacketWithArray_GetSize_CalculatesCorrectly()
    {
        // Arrange
        var packet = new HC_CHARACTER_LIST
        {
            Characters = new[]
            {
                new CharacterInfo { CharId = 1, Name = "Char1", Exp = 1000, Zeny = 500, JobLevel = 10 },
                new CharacterInfo { CharId = 2, Name = "Char2", Exp = 2000, Zeny = 1000, JobLevel = 20 },
                new CharacterInfo { CharId = 3, Name = "Char3", Exp = 3000, Zeny = 1500, JobLevel = 30 }
            }
        };
        
        // Act
        int size = packet.GetSize();
        
        // Assert - Header (2) + Size (2) + Count (1) + 3 * 42 = 131
        Assert.Equal(131, size);
    }
    
    [Fact]
    public void EntityListPacket_GetSize_CalculatesCorrectly()
    {
        // Arrange
        var packet = new ZC_ENTITY_LIST
        {
            Entities = new[]
            {
                new EntityInfo { EntityId = 1, X = 100, Y = 200, EntityType = 1, Name = "Entity1" },
                new EntityInfo { EntityId = 2, X = 150, Y = 250, EntityType = 2, Name = "Entity2" }
            }
        };
        
        // Act
        int size = packet.GetSize();
        
        // Assert - Header (2) + Size (2) + Count (2) + 2 * 33 = 72
        Assert.Equal(72, size);
    }
    
    [Fact]
    public void PacketSize_MatchesSerializedData()
    {
        // Test for all example packets
        var packets = new OutgoingPacket[]
        {
            new AC_ACCEPT_LOGIN { SessionToken = 123, CharacterSlots = 5 },
            new HC_CHARACTER_LIST { Characters = Array.Empty<CharacterInfo>() },
            new ZC_ENTITY_LIST { Entities = Array.Empty<EntityInfo>() }
        };
        
        foreach (var packet in packets)
        {
            // Act - Serialize
            byte[] data;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                packet.Write(writer);
                data = ms.ToArray();
            }
            
            // Assert - Size matches actual data length
            Assert.Equal(packet.GetSize(), data.Length);
        }
    }
}

