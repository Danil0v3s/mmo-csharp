namespace Core.Server.Packets.In.CA;

[PacketVersion(1)]
public class CA_EXE_HASHCHECK : IncomingPacket
{
    private const int SIZE = 18; // header (2) + hash (16)
    
    public byte[] Hash { get; internal set; } = Array.Empty<byte>(); // 16 bytes

    public CA_EXE_HASHCHECK() : base(PacketHeader.CA_EXE_HASHCHECK, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Hash = reader.ReadBytes(16);
    }
}
