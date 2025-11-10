using System.Text;

namespace Core.Server.Packets;

/// <summary>
/// Extension methods for BinaryReader to support packet-specific data types.
/// </summary>
public static class BinaryReaderExtensions
{
    /// <summary>
    /// Reads a fixed-length string using single-byte encoding (ASCII).
    /// Strings are null-terminated or null-padded to the specified length.
    /// </summary>
    /// <param name="reader">The BinaryReader instance</param>
    /// <param name="length">The fixed length to read in bytes</param>
    /// <returns>The decoded string, truncated at first null byte if present</returns>
    public static string ReadFixedString(this BinaryReader reader, int length)
    {
        byte[] bytes = reader.ReadBytes(length);
        
        // Find null terminator
        int nullIndex = Array.IndexOf(bytes, (byte)0);
        int actualLength = nullIndex >= 0 ? nullIndex : length;
        
        // Convert single-byte encoding to string
        return Encoding.ASCII.GetString(bytes, 0, actualLength);
    }
    
    /// <summary>
    /// Reads an array with a count prefix.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <param name="reader">The BinaryReader instance</param>
    /// <param name="readFunc">Function to read a single element</param>
    /// <returns>The array of elements</returns>
    public static T[] ReadArray<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc)
    {
        int count = reader.ReadInt32();
        T[] array = new T[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = readFunc(reader);
        }
        return array;
    }
    
    /// <summary>
    /// Reads an array with a byte count prefix.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <param name="reader">The BinaryReader instance</param>
    /// <param name="readFunc">Function to read a single element</param>
    /// <returns>The array of elements</returns>
    public static T[] ReadByteArray<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc)
    {
        byte count = reader.ReadByte();
        T[] array = new T[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = readFunc(reader);
        }
        return array;
    }
    
    /// <summary>
    /// Reads an array with a short count prefix.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array</typeparam>
    /// <param name="reader">The BinaryReader instance</param>
    /// <param name="readFunc">Function to read a single element</param>
    /// <returns>The array of elements</returns>
    public static T[] ReadShortArray<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc)
    {
        short count = reader.ReadInt16();
        T[] array = new T[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = readFunc(reader);
        }
        return array;
    }
}

