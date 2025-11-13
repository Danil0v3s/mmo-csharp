using System.Text;
using Core.Server.Packets;

namespace Core.Server.Packets.ServerPackets;

[PacketVersion(1)]
public class TC_RESULT : OutgoingPacket
{
    public short PacketLength { get; init; }
    public uint Type { get; init; }
    public string Unknown1 { get; init; } // 20 bytes
    public string Unknown2 { get; init; } // 6 bytes

    public TC_RESULT() : base(PacketHeader.TC_RESULT, isFixedLength: false)
    {
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(PacketLength);
        writer.Write(Type);
        writer.Write(Encoding.UTF8.GetBytes(Unknown1.PadRight(20, '\0')));
        writer.Write(Encoding.UTF8.GetBytes(Unknown2.PadRight(6, '\0')));
    }

    public override int GetSize()
    {
        int baseSize = 2 + 2 + 4 + 20 + 6; // headers and fixed fields
        return baseSize;
    }
}