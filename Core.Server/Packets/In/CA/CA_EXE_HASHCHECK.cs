using System.Text;

namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_EXE_HASHCHECK : IncomingPacket
{
    private const int SIZE = 18; // header (2) + hash (16)
    
    public string Hash { get; internal set; } // 16 bytes

    public CA_EXE_HASHCHECK() : base(PacketHeader.CA_EXE_HASHCHECK, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Hash = Encoding.UTF8.GetString(reader.ReadBytes(16));
    }
}