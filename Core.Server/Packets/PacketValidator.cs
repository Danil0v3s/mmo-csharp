using System.Text;

namespace Core.Server.Packets;

/// <summary>
/// Provides validation methods for packet data to ensure protocol compliance.
/// </summary>
public static class PacketValidator
{
    /// <summary>
    /// Maximum allowed packet size (32KB).
    /// </summary>
    public const int MaxPacketSize = 32768;
    
    /// <summary>
    /// Minimum packet size (header only).
    /// </summary>
    public const int MinPacketSize = 2;
    
    /// <summary>
    /// Validates that a packet size is within the allowed range.
    /// </summary>
    /// <param name="size">The packet size to validate</param>
    /// <exception cref="InvalidDataException">Thrown when size is outside valid range</exception>
    public static void ValidateSize(int size)
    {
        if (size < MinPacketSize || size > MaxPacketSize)
        {
            throw new InvalidDataException(
                $"Packet size {size} is outside valid range [{MinPacketSize}, {MaxPacketSize}]");
        }
    }
    
    /// <summary>
    /// Validates that a string field doesn't exceed its maximum length in bytes.
    /// Uses ASCII encoding (single-byte per character).
    /// </summary>
    /// <param name="value">The string value to validate</param>
    /// <param name="maxLength">Maximum allowed length in bytes</param>
    /// <param name="fieldName">Name of the field for error messages</param>
    /// <exception cref="ArgumentException">Thrown when string exceeds maximum length</exception>
    public static void ValidateStringLength(string? value, int maxLength, string fieldName)
    {
        if (value != null && Encoding.ASCII.GetByteCount(value) > maxLength)
        {
            throw new ArgumentException(
                $"String field '{fieldName}' exceeds maximum length of {maxLength} bytes");
        }
    }
    
    /// <summary>
    /// Validates that an array count is within reasonable bounds.
    /// </summary>
    /// <param name="count">The array count to validate</param>
    /// <param name="elementSize">Size of each array element in bytes</param>
    /// <param name="fieldName">Name of the field for error messages</param>
    /// <exception cref="ArgumentException">Thrown when array would exceed packet size limits</exception>
    public static void ValidateArrayCount(int count, int elementSize, string fieldName)
    {
        if (count < 0)
        {
            throw new ArgumentException($"Array field '{fieldName}' has negative count: {count}");
        }
        
        long totalSize = (long)count * elementSize;
        if (totalSize > MaxPacketSize)
        {
            throw new ArgumentException(
                $"Array field '{fieldName}' with {count} elements would exceed maximum packet size");
        }
    }
}

