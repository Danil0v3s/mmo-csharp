namespace Core.Server.Packets;

/// <summary>
/// Abstract base class for all packet types.
/// Provides common functionality for packet header and length information.
/// </summary>
public abstract class Packet : IPacket
{
    public PacketHeader Header { get; }
    public bool IsFixedLength { get; }
    
    protected Packet(PacketHeader header, bool isFixedLength)
    {
        Header = header;
        IsFixedLength = isFixedLength;
    }
    
    public abstract int GetSize();
}

