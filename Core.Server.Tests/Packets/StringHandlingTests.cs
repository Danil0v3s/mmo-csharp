using Core.Server.Packets;

namespace Core.Server.Tests.Packets;

/// <summary>
/// Tests for string truncation and padding behavior.
/// </summary>
public class StringHandlingTests
{
    [Fact]
    public void WriteFixedString_ExactLength_WritesCorrectly()
    {
        // Arrange
        string value = "12345"; // 5 characters
        int length = 5;
        
        // Act
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.WriteFixedString(value, length);
            data = ms.ToArray();
        }
        
        // Assert
        Assert.Equal(length, data.Length);
        Assert.Equal((byte)'1', data[0]);
        Assert.Equal((byte)'2', data[1]);
        Assert.Equal((byte)'3', data[2]);
        Assert.Equal((byte)'4', data[3]);
        Assert.Equal((byte)'5', data[4]);
    }
    
    [Fact]
    public void WriteFixedString_ShorterThanLength_PadsWithNulls()
    {
        // Arrange
        string value = "ABC"; // 3 characters
        int length = 10;
        
        // Act
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.WriteFixedString(value, length);
            data = ms.ToArray();
        }
        
        // Assert
        Assert.Equal(length, data.Length);
        Assert.Equal((byte)'A', data[0]);
        Assert.Equal((byte)'B', data[1]);
        Assert.Equal((byte)'C', data[2]);
        Assert.Equal(0, data[3]); // Null padding
        Assert.Equal(0, data[4]);
        Assert.Equal(0, data[9]);
    }
    
    [Fact]
    public void WriteFixedString_LongerThanLength_Truncates()
    {
        // Arrange
        string value = "ABCDEFGHIJ"; // 10 characters
        int length = 5;
        
        // Act
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.WriteFixedString(value, length);
            data = ms.ToArray();
        }
        
        // Assert
        Assert.Equal(length, data.Length);
        Assert.Equal((byte)'A', data[0]);
        Assert.Equal((byte)'B', data[1]);
        Assert.Equal((byte)'C', data[2]);
        Assert.Equal((byte)'D', data[3]);
        Assert.Equal((byte)'E', data[4]);
    }
    
    [Fact]
    public void ReadFixedString_NullTerminated_ReadsUntilNull()
    {
        // Arrange
        byte[] data = new byte[] { (byte)'T', (byte)'e', (byte)'s', (byte)'t', 0, 0, 0, 0, 0, 0 };
        
        // Act
        string result;
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            result = reader.ReadFixedString(10);
        }
        
        // Assert
        Assert.Equal("Test", result);
    }
    
    [Fact]
    public void ReadFixedString_NoNullTerminator_ReadsFullLength()
    {
        // Arrange
        byte[] data = new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E' };
        
        // Act
        string result;
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            result = reader.ReadFixedString(5);
        }
        
        // Assert
        Assert.Equal("ABCDE", result);
    }
    
    [Fact]
    public void StringRoundTrip_PreservesValue()
    {
        // Arrange
        string original = "MyUsername";
        int length = 24;
        
        // Act - Write
        byte[] data;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.WriteFixedString(original, length);
            data = ms.ToArray();
        }
        
        // Act - Read
        string result;
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            result = reader.ReadFixedString(length);
        }
        
        // Assert
        Assert.Equal(original, result);
        Assert.Equal(length, data.Length);
    }
    
    [Fact]
    public void WriteFixedString_NullOrEmpty_WritesNullBytes()
    {
        // Arrange & Act - Null string
        byte[] nullData;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.WriteFixedString(null!, 5);
            nullData = ms.ToArray();
        }
        
        // Assert - All zeros
        Assert.Equal(5, nullData.Length);
        Assert.All(nullData, b => Assert.Equal(0, b));
        
        // Arrange & Act - Empty string
        byte[] emptyData;
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.WriteFixedString(string.Empty, 5);
            emptyData = ms.ToArray();
        }
        
        // Assert - All zeros
        Assert.Equal(5, emptyData.Length);
        Assert.All(emptyData, b => Assert.Equal(0, b));
    }
}

