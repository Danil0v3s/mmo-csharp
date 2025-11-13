using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.In.CA;
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
        var original = new CA_LOGIN();
        
        // Act - Serialize with manual write (since CA_LOGIN is incoming packet)
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((short)PacketHeader.CA_LOGIN);
            writer.Write((uint)1); // Version
            writer.WriteFixedString("testuser", 24);
            writer.WriteFixedString("testpass", 24);
            writer.Write((byte)5);
            data = ms.ToArray();
        }
        
        // Assert - Check size
        Assert.Equal(55, data.Length);
        Assert.Equal(55, original.GetSize());
        
        // Assert - Verify byte layout (little-endian)
        Assert.Equal(0x64, data[0]); // Header low byte (0x64)
        Assert.Equal(0x00, data[1]); // Header high byte
        Assert.Equal(0x01, data[2]); // Version byte 0
        Assert.Equal(0x00, data[3]); // Version byte 1
        Assert.Equal(0x00, data[4]); // Version byte 2
        Assert.Equal(0x00, data[5]); // Version byte 3
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
        var original = new HC_CHARACTER_LIST
        {
            Characters = new[]
            {
                new CharacterInfo
                {
                    CharId = 1,
                    Exp = 1000,
                    Zeny = 500,
                    JobLevel = 10,
                    Name = "TestChar"
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
        
        // Assert - Check size
        Assert.Equal(47, data.Length);
        Assert.Equal(47, original.GetSize());
        Assert.False(original.IsFixedLength);
        
        // Deserialize
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            short header = reader.ReadInt16();
            short size = reader.ReadInt16();
            byte count = reader.ReadByte();
            
            Assert.Equal((short)PacketHeader.HC_CHARACTER_LIST, header);
            Assert.Equal(47, size);
            Assert.Equal(1, count);
            
            var character = CharacterInfo.Read(reader);
            Assert.Equal(original.Characters[0].CharId, character.CharId);
            Assert.Equal(original.Characters[0].Name, character.Name);
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

