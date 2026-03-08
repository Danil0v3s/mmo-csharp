namespace Core.Server.Packets;

/// <summary>
/// Base class for packets sent to clients.
/// Outgoing packets implement the Write method to serialize only their body fields.
/// </summary>
public abstract class OutgoingPacket : Packet
{
    protected OutgoingPacket(PacketHeader header, int size) 
        : base(header, size)
    {
    }
    
    /// <summary>
    /// Whether this packet includes an explicit packet-length field after the header.
    /// Variable-length packets do by default; fixed-length packets may opt in explicitly.
    /// </summary>
    public virtual bool HasPacketLength => Size == -1;

    /// <summary>
    /// Writes only the packet body to the provided BinaryWriter.
    /// </summary>
    /// <param name="writer">BinaryWriter to write the packet to</param>
    public abstract void Write(BinaryWriter writer);

    public void WritePacket(BinaryWriter writer)
    {
        int size = GetSize();
        PacketValidator.ValidateSize(size);

        writer.Write((short)Header);
        if (HasPacketLength)
        {
            writer.Write((short)size);
        }

        Write(writer);
    }
    
    /// <summary>
    /// Gets the actual size of this packet instance.
    /// For fixed-length packets, returns Size from the base class.
    /// For variable-length packets, override to calculate the actual size.
    /// </summary>
    public virtual int GetSize() => Size;
}
