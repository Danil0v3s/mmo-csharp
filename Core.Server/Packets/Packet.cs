namespace Core.Server.Packets;

/// <summary>
/// Abstract base class for all packet types.
/// Provides common functionality for packet header and length information.
/// </summary>
public abstract class Packet : IPacket
{
    public PacketHeader Header { get; }
    public int Size { get; }
    
    protected Packet(PacketHeader header, int size)
    {
        Header = header;
        Size = size;
    }
}

