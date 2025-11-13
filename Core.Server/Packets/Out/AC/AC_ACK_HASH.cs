using System.Text;

namespace Core.Server.Packets.Out.AC;

[PacketVersion(1)]
public class AC_ACK_HASH : OutgoingPacket
{
    public short PacketLength { get; init; }
    public string Salt { get; init; }

    public AC_ACK_HASH() : base(PacketHeader.AC_ACK_HASH, isFixedLength: false)
    {
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(PacketLength);
        writer.Write(Encoding.UTF8.GetBytes(Salt));
    }

    public override int GetSize()
    {
        int headerSize = 2 + 2; // packetType + packetLength
        int bodySize = Encoding.UTF8.GetByteCount(Salt);
        return headerSize + bodySize;
    }
}