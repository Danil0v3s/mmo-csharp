using Core.Server.Packets;
using Core.Server.Packets.ServerPackets;
using Xunit;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests for packet validation including size limits and data validation.
/// </summary>
public class PacketValidationTests
{
    [Fact]
    public void ValidateSize_ValidSize_DoesNotThrow()
    {
        // Arrange
        int validSize = 100;
        
        // Act & Assert
        PacketValidator.ValidateSize(validSize);
    }
    
    [Fact]
    public void ValidateSize_MinimumSize_DoesNotThrow()
    {
        // Arrange
        int minSize = PacketValidator.MinPacketSize;
        
        // Act & Assert
        PacketValidator.ValidateSize(minSize);
    }
    
    [Fact]
    public void ValidateSize_MaximumSize_DoesNotThrow()
    {
        // Arrange
        int maxSize = PacketValidator.MaxPacketSize;
        
        // Act & Assert
        PacketValidator.ValidateSize(maxSize);
    }
    
    [Fact]
    public void ValidateSize_TooSmall_ThrowsException()
    {
        // Arrange
        int tooSmall = PacketValidator.MinPacketSize - 1;
        
        // Act & Assert
        Assert.Throws<InvalidDataException>(() => PacketValidator.ValidateSize(tooSmall));
    }
    
    [Fact]
    public void ValidateSize_TooLarge_ThrowsException()
    {
        // Arrange
        int tooLarge = PacketValidator.MaxPacketSize + 1;
        
        // Act & Assert
        Assert.Throws<InvalidDataException>(() => PacketValidator.ValidateSize(tooLarge));
    }
    
    [Fact]
    public void ValidateStringLength_ValidLength_DoesNotThrow()
    {
        // Arrange
        string value = "TestString";
        int maxLength = 24;
        
        // Act & Assert
        PacketValidator.ValidateStringLength(value, maxLength, "TestField");
    }
    
    [Fact]
    public void ValidateStringLength_ExactMaxLength_DoesNotThrow()
    {
        // Arrange
        string value = new string('A', 24);
        int maxLength = 24;
        
        // Act & Assert
        PacketValidator.ValidateStringLength(value, maxLength, "TestField");
    }
    
    [Fact]
    public void ValidateStringLength_TooLong_ThrowsException()
    {
        // Arrange
        string value = new string('A', 25);
        int maxLength = 24;
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            PacketValidator.ValidateStringLength(value, maxLength, "TestField"));
    }
    
    [Fact]
    public void ValidateStringLength_NullString_DoesNotThrow()
    {
        // Arrange
        string? value = null;
        int maxLength = 24;
        
        // Act & Assert
        PacketValidator.ValidateStringLength(value, maxLength, "TestField");
    }
    
    [Fact]
    public void ValidateArrayCount_ValidCount_DoesNotThrow()
    {
        // Arrange
        int count = 10;
        int elementSize = 42;
        
        // Act & Assert
        PacketValidator.ValidateArrayCount(count, elementSize, "TestArray");
    }
    
    [Fact]
    public void ValidateArrayCount_ZeroCount_DoesNotThrow()
    {
        // Arrange
        int count = 0;
        int elementSize = 42;
        
        // Act & Assert
        PacketValidator.ValidateArrayCount(count, elementSize, "TestArray");
    }
    
    [Fact]
    public void ValidateArrayCount_NegativeCount_ThrowsException()
    {
        // Arrange
        int count = -1;
        int elementSize = 42;
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            PacketValidator.ValidateArrayCount(count, elementSize, "TestArray"));
    }
    
    [Fact]
    public void ValidateArrayCount_ExceedsMaxPacketSize_ThrowsException()
    {
        // Arrange
        int count = 1000;
        int elementSize = 1000; // Would result in 1,000,000 bytes
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            PacketValidator.ValidateArrayCount(count, elementSize, "TestArray"));
    }
    
    [Fact]
    public void MaxPacketSize_Is32KB()
    {
        // Assert
        Assert.Equal(32768, PacketValidator.MaxPacketSize);
    }
    
    [Fact]
    public void MinPacketSize_Is2Bytes()
    {
        // Assert
        Assert.Equal(2, PacketValidator.MinPacketSize);
    }
}

