using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.In.CA;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Configuration;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests for the integrated packet system.
/// </summary>
public class PacketSystemTests
{
    [Fact]
    public void Initialize_WithoutConfiguration_Succeeds()
    {
        // Arrange
        var packetSystem = new PacketSystem();
        
        // Act
        packetSystem.Initialize();
        
        // Assert
        Assert.True(packetSystem.Factory.CanCreatePacket(PacketHeader.CA_LOGIN));
        Assert.True(packetSystem.Registry.IsRegistered(PacketHeader.CZ_HEARTBEAT));
    }
    
    [Fact]
    public void Initialize_WithConfiguration_AppliesVersions()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["PacketVersions:CA_LOGIN"] = "1",
            ["PacketVersions:CZ_HEARTBEAT"] = "1"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
        
        var packetSystem = new PacketSystem();
        
        // Act
        packetSystem.Initialize(configuration);
        
        // Assert
        Assert.Equal(1, packetSystem.Factory.GetActiveVersion(PacketHeader.CA_LOGIN));
        Assert.Equal(1, packetSystem.Factory.GetActiveVersion(PacketHeader.CZ_HEARTBEAT));
    }
    
    [Fact]
    public void WritePacket_ValidPacket_WritesCorrectly()
    {
        // Arrange
        var packetSystem = new PacketSystem();
        packetSystem.Initialize();
        
        var packet = new HC_CHARACTER_LIST
        {
            Characters = Array.Empty<CharacterInfo>()
        };
        
        // Act
        byte[] data = packetSystem.SerializePacket(packet);
        
        // Assert
        Assert.Equal(5, data.Length);
        Assert.Equal(packet.GetSize(), data.Length);
    }
    
    [Fact]
    public void ReadPacket_FixedLengthPacket_ReadsCorrectly()
    {
        // Arrange
        var packetSystem = new PacketSystem();
        packetSystem.Initialize();
        
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((short)PacketHeader.CZ_HEARTBEAT);
            data = ms.ToArray();
        }
        
        // Act
        IncomingPacket? packet;
        using (var ms = new MemoryStream(data))
        {
            packet = packetSystem.ReadPacket(ms);
        }
        
        // Assert
        Assert.NotNull(packet);
        Assert.IsType<CZ_HEARTBEAT>(packet);
        Assert.Equal(PacketHeader.CZ_HEARTBEAT, packet.Header);
    }
    
    [Fact]
    public void ReadPacket_FixedLengthLoginPacket_ReadsCorrectly()
    {
        // Arrange
        var packetSystem = new PacketSystem();
        packetSystem.Initialize();
        
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((short)PacketHeader.CA_LOGIN);
            writer.Write((uint)1); // Version
            writer.WriteFixedString("TestUser", 24);
            writer.WriteFixedString("TestPass", 24);
            writer.Write((byte)2);
            data = ms.ToArray();
        }
        
        // Act
        IncomingPacket? packet;
        using (var ms = new MemoryStream(data))
        {
            packet = packetSystem.ReadPacket(ms);
        }
        
        // Assert
        Assert.NotNull(packet);
        var loginPacket = Assert.IsType<CA_LOGIN>(packet);
        Assert.Equal("TestUser", loginPacket.Username);
        Assert.Equal("TestPass", loginPacket.Password);
        Assert.Equal(2, loginPacket.Clienttype);
    }
    
    [Fact]
    public void ReadPacket_InvalidHeader_ThrowsException()
    {
        // Arrange
        var packetSystem = new PacketSystem();
        packetSystem.Initialize();
        
        byte[] data = new byte[] { 0xFF, 0xFF }; // Invalid header
        
        // Act & Assert
        using (var ms = new MemoryStream(data))
        {
            Assert.Throws<InvalidDataException>(() => packetSystem.ReadPacket(ms));
        }
    }
    
    [Fact]
    public void SerializePacket_ComplexPacket_ProducesCorrectBytes()
    {
        // Arrange
        var packetSystem = new PacketSystem();
        packetSystem.Initialize();
        
        var packet = new HC_CHARACTER_LIST
        {
            Characters = new[]
            {
                new CharacterInfo
                {
                    CharId = 1,
                    Exp = 1000,
                    Zeny = 500,
                    JobLevel = 10,
                    Name = "Hero"
                }
            }
        };
        
        // Act
        byte[] data = packetSystem.SerializePacket(packet);
        
        // Assert
        int expectedSize = 2 + 2 + 1 + 42; // header + size + count + 1 character
        Assert.Equal(expectedSize, data.Length);
    }
    
    [Fact]
    public void PacketRoundTrip_ThroughSystem_PreservesData()
    {
        // Arrange
        var writeSystem = new PacketSystem();
        writeSystem.Initialize();
        
        var originalPacket = new ZC_ENTITY_LIST
        {
            Entities = new[]
            {
                new EntityInfo { EntityId = 100, X = 50, Y = 75, EntityType = 1, Name = "TestEntity" }
            }
        };
        
        // Act - Serialize
        byte[] data = writeSystem.SerializePacket(originalPacket);
        
        // Assert
        Assert.Equal(originalPacket.GetSize(), data.Length);
        
        // Verify we can at least read the header and size
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            short header = reader.ReadInt16();
            short size = reader.ReadInt16();
            short count = reader.ReadInt16();
            
            Assert.Equal((short)PacketHeader.ZC_ENTITY_LIST, header);
            Assert.Equal(originalPacket.GetSize(), size);
            Assert.Equal(1, count);
            
            var entity = EntityInfo.Read(reader);
            Assert.Equal(100, entity.EntityId);
            Assert.Equal("TestEntity", entity.Name);
        }
    }
}

