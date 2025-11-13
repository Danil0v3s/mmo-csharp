using System.Text;

namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_EXE_HASHCHECK : IncomingPacket
{
    public string Hash { get; internal set; } // 16 bytes

    public CA_EXE_HASHCHECK() : base(PacketHeader.CA_EXE_HASHCHECK, isFixedLength: true)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Hash = Encoding.UTF8.GetString(reader.ReadBytes(16));
    }
    
    public override int GetSize()
    {
        // header (2) + hash (16)
        return 2 + 16;
    }
}