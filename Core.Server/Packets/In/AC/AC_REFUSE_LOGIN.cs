namespace Core.Server.Packets.In.AC;

[PacketVersion(1)]
public class AC_REFUSE_LOGIN : IncomingPacket
{
    private const int SIZE = 26;

    public uint Error { get; internal set; }
    public string UnblockTime { get; internal set; } = string.Empty;

    public AC_REFUSE_LOGIN() : base(PacketHeader.AC_REFUSE_LOGIN, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Error = reader.ReadUInt32();
        UnblockTime = reader.ReadFixedString(20);
    }
}
