namespace Core.Server.Packets.ServerPackets;

/// <summary>
/// Entity information structure for entity list packets.
/// Represents a single entity (player, NPC, monster) in the game world.
/// </summary>
public struct EntityInfo
{
    private const int NameLength = 24;
    
    public int EntityId { get; init; }
    public short X { get; init; }
    public short Y { get; init; }
    public byte EntityType { get; init; }
    public string Name { get; init; } // 24 bytes
    
    /// <summary>
    /// Size of this structure in bytes when serialized.
    /// </summary>
    public static int Size => 4 + 2 + 2 + 1 + NameLength; // 33 bytes
    
    /// <summary>
    /// Writes this structure to a BinaryWriter.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(EntityType);
        writer.WriteFixedString(Name ?? string.Empty, NameLength);
    }
    
    /// <summary>
    /// Reads this structure from a BinaryReader.
    /// </summary>
    public static EntityInfo Read(BinaryReader reader)
    {
        return new EntityInfo
        {
            EntityId = reader.ReadInt32(),
            X = reader.ReadInt16(),
            Y = reader.ReadInt16(),
            EntityType = reader.ReadByte(),
            Name = reader.ReadFixedString(NameLength)
        };
    }
}

/// <summary>
/// Example 5: Complex nested structure packet.
/// Server sends this to client with entities visible in the current area.
/// Packet structure: [Header (2)] [Size (2)] [Count (2)] [Entities (Count * 33)]
/// Minimum size: 6 bytes (no entities)
/// </summary>
[PacketVersion(1)]
public class ZC_ENTITY_LIST : OutgoingPacket
{
    public EntityInfo[] Entities { get; init; } = Array.Empty<EntityInfo>();
    
    public ZC_ENTITY_LIST() : base(PacketHeader.ZC_ENTITY_LIST, -1)
    {
    }
    
    public override void Write(BinaryWriter writer)
    {
        int size = GetSize();
        PacketValidator.ValidateSize(size);
        PacketValidator.ValidateArrayCount(Entities.Length, EntityInfo.Size, nameof(Entities));
        
        writer.Write((short)Header);           // 2 bytes
        writer.Write((short)size);             // 2 bytes
        writer.Write((short)Entities.Length);  // 2 bytes (count)
        
        foreach (var entity in Entities)
        {
            entity.Write(writer);
        }
    }
    
    public override int GetSize() => 2 + 2 + 2 + (Entities.Length * EntityInfo.Size);
}

