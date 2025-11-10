using Core.Server.Packets;
using Core.Server.Packets.ServerPackets;
using Xunit;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests to verify little-endian byte order in packet serialization.
/// </summary>
public class EndiannessTests
{
    [Fact]
    public void ShortValue_WrittenAsLittleEndian()
    {
        // Arrange
        short value = 0x1234;
        
        // Act
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(value);
            data = ms.ToArray();
        }
        
        // Assert - Little-endian: low byte first, high byte second
        Assert.Equal(2, data.Length);
        Assert.Equal(0x34, data[0]); // Low byte
        Assert.Equal(0x12, data[1]); // High byte
    }
    
    [Fact]
    public void IntValue_WrittenAsLittleEndian()
    {
        // Arrange
        int value = 0x12345678;
        
        // Act
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(value);
            data = ms.ToArray();
        }
        
        // Assert - Little-endian: lowest byte first
        Assert.Equal(4, data.Length);
        Assert.Equal(0x78, data[0]); // Lowest byte
        Assert.Equal(0x56, data[1]);
        Assert.Equal(0x34, data[2]);
        Assert.Equal(0x12, data[3]); // Highest byte
    }
    
    [Fact]
    public void LongValue_WrittenAsLittleEndian()
    {
        // Arrange
        long value = 0x123456789ABCDEF0L;
        
        // Act
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(value);
            data = ms.ToArray();
        }
        
        // Assert - Little-endian: lowest byte first
        Assert.Equal(8, data.Length);
        Assert.Equal(0xF0, data[0]); // Lowest byte
        Assert.Equal(0xDE, data[1]);
        Assert.Equal(0xBC, data[2]);
        Assert.Equal(0x9A, data[3]);
        Assert.Equal(0x78, data[4]);
        Assert.Equal(0x56, data[5]);
        Assert.Equal(0x34, data[6]);
        Assert.Equal(0x12, data[7]); // Highest byte
    }
    
    [Fact]
    public void PacketHeader_WrittenAsLittleEndian()
    {
        // Arrange
        var packet = new AC_ACCEPT_LOGIN
        {
            SessionToken = 0x11223344,
            CharacterSlots = 5
        };
        
        // Act
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            packet.Write(writer);
            data = ms.ToArray();
        }
        
        // Assert - Check header (0x0069) in little-endian
        Assert.Equal(0x69, data[0]); // Low byte
        Assert.Equal(0x00, data[1]); // High byte
        
        // Assert - Check SessionToken (0x11223344) in little-endian
        Assert.Equal(0x44, data[2]); // Lowest byte
        Assert.Equal(0x33, data[3]);
        Assert.Equal(0x22, data[4]);
        Assert.Equal(0x11, data[5]); // Highest byte
    }
    
    [Fact]
    public void ReadWriteRoundTrip_PreservesLittleEndian()
    {
        // Arrange
        int originalInt = 0x12345678;
        short originalShort = 0x4321;
        long originalLong = 0x0011223344556677L;
        
        // Act - Write
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(originalInt);
            writer.Write(originalShort);
            writer.Write(originalLong);
            data = ms.ToArray();
        }
        
        // Act - Read
        int readInt;
        short readShort;
        long readLong;
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            readInt = reader.ReadInt32();
            readShort = reader.ReadInt16();
            readLong = reader.ReadInt64();
        }
        
        // Assert
        Assert.Equal(originalInt, readInt);
        Assert.Equal(originalShort, readShort);
        Assert.Equal(originalLong, readLong);
    }
}

