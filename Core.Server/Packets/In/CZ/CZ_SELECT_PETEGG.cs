namespace Core.Server.Packets.In.CZ;

/// <summary>
/// The player picked an egg to hatch from the egg list. rAthena <c>clif_parse_SelectEgg</c>
/// (clif.cpp, 0x01a7). Fixed 4 bytes: <c>01a7 &lt;index&gt;.W</c> — the egg's client inventory index
/// (server index + 2).
/// </summary>
public class CZ_SELECT_PETEGG : IncomingPacket
{
    private const int SIZE = 4;

    public short Index { get; private set; }

    public CZ_SELECT_PETEGG() : base(PacketHeader.CZ_SELECT_PETEGG, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Index = reader.ReadInt16();
    }

    public static CZ_SELECT_PETEGG Create(BinaryReader reader)
    {
        var packet = new CZ_SELECT_PETEGG();
        packet.Read(reader);
        return packet;
    }
}
