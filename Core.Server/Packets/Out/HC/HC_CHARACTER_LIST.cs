namespace Core.Server.Packets.ServerPackets;

/// <summary>
/// Character information structure for character list packets.
/// </summary>
public struct CharacterInfo
{
    private const int NameLength = 24;
    
    public int CharId { get; init; }
    public long Exp { get; init; }
    public int Zeny { get; init; }
    public short JobLevel { get; init; }
    public string Name { get; init; } // 24 bytes
    
    /// <summary>
    /// Size of this structure in bytes when serialized.
    /// </summary>
    public static int Size => 4 + 8 + 4 + 2 + NameLength; // 42 bytes
    
    /// <summary>
    /// Writes this structure to a BinaryWriter.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(CharId);
        writer.Write(Exp);
        writer.Write(Zeny);
        writer.Write(JobLevel);
        writer.WriteFixedString(Name ?? string.Empty, NameLength);
    }
    
    /// <summary>
    /// Reads this structure from a BinaryReader.
    /// </summary>
    public static CharacterInfo Read(BinaryReader reader)
    {
        return new CharacterInfo
        {
            CharId = reader.ReadInt32(),
            Exp = reader.ReadInt64(),
            Zeny = reader.ReadInt32(),
            JobLevel = reader.ReadInt16(),
            Name = reader.ReadFixedString(NameLength)
        };
    }
}

/// <summary>
/// Example 4: Variable-length outgoing packet with array.
/// Server sends this to client with the list of characters on the account.
/// Packet structure: [Header (2)] [Size (2)] [Count (1)] [Characters (Count * 42)]
/// Minimum size: 5 bytes (no characters)
/// </summary>
[PacketVersion(1)]
public class HC_CHARACTER_LIST : OutgoingPacket
{
    public CharacterInfo[] Characters { get; init; } = Array.Empty<CharacterInfo>();
    
    public HC_CHARACTER_LIST() : base(PacketHeader.HC_CHARACTER_LIST, isFixedLength: false)
    {
    }
    
    public override void Write(BinaryWriter writer)
    {
        int size = GetSize();
        PacketValidator.ValidateSize(size);
        PacketValidator.ValidateArrayCount(Characters.Length, CharacterInfo.Size, nameof(Characters));
        
        writer.Write((short)Header);              // 2 bytes
        writer.Write((short)size);                // 2 bytes (total size)
        writer.Write((byte)Characters.Length);    // 1 byte (count)
        
        foreach (var character in Characters)
        {
            character.Write(writer);
        }
    }
    
    public override int GetSize() => 2 + 2 + 1 + (Characters.Length * CharacterInfo.Size);
}

