namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Request to play a pet emotion / act. rAthena <c>clif_parse_SendEmotion</c> (clif.cpp, 0x01a9).
/// Fixed 6 bytes: <c>01a9 &lt;data&gt;.L</c> — either an emotion id or a compound act/hunger value.
/// </summary>
public class CZ_PET_ACT : IncomingPacket
{
    private const int SIZE = 6;

    public int Data { get; private set; }

    public CZ_PET_ACT() : base(PacketHeader.CZ_PET_ACT, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Data = reader.ReadInt32();
    }

    public static CZ_PET_ACT Create(BinaryReader reader)
    {
        var packet = new CZ_PET_ACT();
        packet.Read(reader);
        return packet;
    }
}
