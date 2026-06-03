namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Change the party EXP-share option (leader only). rAthena <c>clif_parse_PartyChangeOption</c>
/// (clif.cpp, the 0x0102 form). Wire: <c>0102 &lt;expflag&gt;.L</c> — 6 bytes. The item-share field is
/// not changed by this packet (rAthena keeps the existing party item policy).
/// </summary>
public class CZ_PARTY_CHANGE_OPTION : IncomingPacket
{
    private const int SIZE = 6;

    public int ExpFlag { get; private set; }

    public CZ_PARTY_CHANGE_OPTION() : base(PacketHeader.CZ_PARTY_CHANGE_OPTION, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ExpFlag = reader.ReadInt32();
    }

    public static CZ_PARTY_CHANGE_OPTION Create(BinaryReader reader)
    {
        var packet = new CZ_PARTY_CHANGE_OPTION();
        packet.Read(reader);
        return packet;
    }
}
