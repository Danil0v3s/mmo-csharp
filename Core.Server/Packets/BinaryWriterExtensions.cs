using System.Text;

namespace Core.Server.Packets;

/// <summary>
/// Extension methods for BinaryWriter to support packet-specific data types.
/// </summary>
public static class BinaryWriterExtensions
{
    /// <summary>
    /// Writes a fixed-length string using single-byte encoding (ASCII).
    /// Truncates if the string is too long, pads with null bytes if too short.
    /// </summary>
    /// <param name="writer">The BinaryWriter instance</param>
    /// <param name="value">The string value to write (can be null or empty)</param>
    /// <param name="length">The fixed length to write in bytes</param>
    public static void WriteFixedString(this BinaryWriter writer, string value, int length)
    {
        byte[] bytes = new byte[length];
        
        if (!string.IsNullOrEmpty(value))
        {
            byte[] stringBytes = Encoding.ASCII.GetBytes(value);
            int copyLength = Math.Min(stringBytes.Length, length);
            Array.Copy(stringBytes, bytes, copyLength);
        }
        
        writer.Write(bytes);
    }
    
    /// <summary>
    /// Writes an array with a count prefix (4 bytes).
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <param name="writer">The BinaryWriter instance</param>
    /// <param name="array">The array to write</param>
    /// <param name="writeFunc">Function to write a single element</param>
    public static void WriteArray<T>(this BinaryWriter writer, T[] array, Action<BinaryWriter, T> writeFunc)
    {
        writer.Write(array.Length);
        foreach (T item in array)
        {
            writeFunc(writer, item);
        }
    }
    
    /// <summary>
    /// Writes an array with a byte count prefix (1 byte).
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <param name="writer">The BinaryWriter instance</param>
    /// <param name="array">The array to write</param>
    /// <param name="writeFunc">Function to write a single element</param>
    public static void WriteByteArray<T>(this BinaryWriter writer, T[] array, Action<BinaryWriter, T> writeFunc)
    {
        if (array.Length > byte.MaxValue)
            throw new ArgumentException($"Array length {array.Length} exceeds byte max value {byte.MaxValue}");
            
        writer.Write((byte)array.Length);
        foreach (T item in array)
        {
            writeFunc(writer, item);
        }
    }
    
    /// <summary>
    /// Writes an array with a short count prefix (2 bytes).
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <param name="writer">The BinaryWriter instance</param>
    /// <param name="array">The array to write</param>
    /// <param name="writeFunc">Function to write a single element</param>
    public static void WriteShortArray<T>(this BinaryWriter writer, T[] array, Action<BinaryWriter, T> writeFunc)
    {
        if (array.Length > short.MaxValue)
            throw new ArgumentException($"Array length {array.Length} exceeds short max value {short.MaxValue}");
            
        writer.Write((short)array.Length);
        foreach (T item in array)
        {
            writeFunc(writer, item);
        }
    }
}

