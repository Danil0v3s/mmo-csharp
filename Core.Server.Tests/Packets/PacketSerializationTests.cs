using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests for packet serialization and deserialization (round-trip tests).
/// Verifies that packets can be written and read back correctly.
/// </summary>
public class PacketSerializationTests
{
    [Fact]
    public void FixedLengthPacket_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new AC_ACCEPT_LOGIN
        {
            SessionToken = 12345678,
            CharacterSlots = 9
        };
        
        // Act - Serialize
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            original.Write(writer);
            data = ms.ToArray();
        }
        
        // Assert - Check size
        Assert.Equal(7, data.Length);
        Assert.Equal(7, original.GetSize());
        
        // Assert - Verify byte layout (little-endian)
        Assert.Equal(0x69, data[0]); // Header low byte
        Assert.Equal(0x00, data[1]); // Header high byte
        Assert.Equal(0x4E, data[2]); // SessionToken byte 0
        Assert.Equal(0x61, data[3]); // SessionToken byte 1
        Assert.Equal(0xBC, data[4]); // SessionToken byte 2
        Assert.Equal(0x00, data[5]); // SessionToken byte 3
        Assert.Equal(9, data[6]);    // CharacterSlots
    }
    
    [Fact]
    public void FixedLengthHeartbeatPacket_RoundTrip_Success()
    {
        // Arrange & Act - Create heartbeat
        var heartbeat = new CZ_HEARTBEAT();
        
        // Act - Serialize
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((short)heartbeat.Header);
            data = ms.ToArray();
        }
        
        // Assert
        Assert.Equal(2, data.Length);
        Assert.Equal(2, heartbeat.GetSize());
        Assert.True(heartbeat.IsFixedLength);
        
        // Deserialize
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            short header = reader.ReadInt16();
            Assert.Equal((short)PacketHeader.CZ_HEARTBEAT, header);
            
            var packet = CZ_HEARTBEAT.Create(reader);
            Assert.NotNull(packet);
            Assert.Equal(PacketHeader.CZ_HEARTBEAT, packet.Header);
        }
    }
    
    [Fact]
    public void VariableLengthPacket_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new CA_LOGIN
        {
            Username = "TestUser",
            Password = "SecretPass",
            ClientType = 3
        };
        
        // Act - Serialize
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((short)original.Header);
            writer.Write((short)original.GetSize());
            writer.WriteFixedString(original.Username, 24);
            writer.WriteFixedString(original.Password, 24);
            writer.Write(original.ClientType);
            data = ms.ToArray();
        }
        
        // Assert - Check size
        Assert.Equal(53, data.Length);
        Assert.Equal(53, original.GetSize());
        Assert.False(original.IsFixedLength);
        
        // Deserialize
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            short header = reader.ReadInt16();
            short size = reader.ReadInt16();
            
            Assert.Equal((short)PacketHeader.CA_LOGIN, header);
            Assert.Equal(53, size);
            
            var deserialized = CA_LOGIN.Create(reader);
            Assert.Equal(original.Username, deserialized.Username);
            Assert.Equal(original.Password, deserialized.Password);
            Assert.Equal(original.ClientType, deserialized.ClientType);
        }
    }
    
    [Fact]
    public void PacketWithArray_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new HC_CHARACTER_LIST
        {
            Characters = new[]
            {
                new CharacterInfo
                {
                    CharId = 1,
                    Exp = 1000000L,
                    Zeny = 50000,
                    JobLevel = 50,
                    Name = "Warrior"
                },
                new CharacterInfo
                {
                    CharId = 2,
                    Exp = 500000L,
                    Zeny = 25000,
                    JobLevel = 30,
                    Name = "Mage"
                }
            }
        };
        
        // Act - Serialize
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            original.Write(writer);
            data = ms.ToArray();
        }
        
        // Assert - Check size (header=2, size=2, count=1, 2 characters * 42 bytes each)
        int expectedSize = 2 + 2 + 1 + (2 * 42);
        Assert.Equal(expectedSize, data.Length);
        Assert.Equal(expectedSize, original.GetSize());
        
        // Deserialize
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            short header = reader.ReadInt16();
            short size = reader.ReadInt16();
            byte count = reader.ReadByte();
            
            Assert.Equal((short)PacketHeader.HC_CHARACTER_LIST, header);
            Assert.Equal(expectedSize, size);
            Assert.Equal(2, count);
            
            var chars = new CharacterInfo[count];
            for (int i = 0; i < count; i++)
            {
                chars[i] = CharacterInfo.Read(reader);
            }
            
            Assert.Equal(original.Characters[0].CharId, chars[0].CharId);
            Assert.Equal(original.Characters[0].Name, chars[0].Name);
            Assert.Equal(original.Characters[1].CharId, chars[1].CharId);
            Assert.Equal(original.Characters[1].Name, chars[1].Name);
        }
    }
    
    [Fact]
    public void ComplexNestedStructure_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new ZC_ENTITY_LIST
        {
            Entities = new[]
            {
                new EntityInfo
                {
                    EntityId = 100,
                    X = 150,
                    Y = 200,
                    EntityType = 1,
                    Name = "PlayerOne"
                },
                new EntityInfo
                {
                    EntityId = 101,
                    X = 155,
                    Y = 205,
                    EntityType = 2,
                    Name = "MonsterA"
                }
            }
        };
        
        // Act - Serialize
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            original.Write(writer);
            data = ms.ToArray();
        }
        
        // Assert - Check size (header=2, size=2, count=2, 2 entities * 33 bytes each)
        int expectedSize = 2 + 2 + 2 + (2 * 33);
        Assert.Equal(expectedSize, data.Length);
        Assert.Equal(expectedSize, original.GetSize());
        
        // Deserialize
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            short header = reader.ReadInt16();
            short size = reader.ReadInt16();
            short count = reader.ReadInt16();
            
            Assert.Equal((short)PacketHeader.ZC_ENTITY_LIST, header);
            Assert.Equal(expectedSize, size);
            Assert.Equal(2, count);
            
            var entities = new EntityInfo[count];
            for (int i = 0; i < count; i++)
            {
                entities[i] = EntityInfo.Read(reader);
            }
            
            Assert.Equal(original.Entities[0].EntityId, entities[0].EntityId);
            Assert.Equal(original.Entities[0].Name, entities[0].Name);
            Assert.Equal(original.Entities[1].X, entities[1].X);
            Assert.Equal(original.Entities[1].Y, entities[1].Y);
        }
    }
}

