namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Action request" — attack / sit / stand. rAthena
/// <c>clif_parse_ActionRequest</c> (clif.cpp:11782).
///
/// Wire format: <c>0437 &lt;target id&gt;.L &lt;action&gt;.B</c> (PACKETVER
/// shuffle from the original <c>0089</c>). Total size: 2 (header) + 4
/// (target_id) + 1 (action) = 7 bytes.
///
/// <see cref="Action"/> values mirror rAthena <c>damage_lv</c> visual
/// codes for the parse path:
/// <list type="bullet">
///   <item>0 = single attack</item>
///   <item>1 = pick up item</item>
///   <item>2 = sit down</item>
///   <item>3 = stand up</item>
///   <item>7 = continuous attack (autoattack)</item>
/// </list>
/// </summary>
public class CZ_REQUEST_ACTION : IncomingPacket
{
    private const int SIZE = 7;

    public int TargetId { get; private set; }
    public byte Action { get; private set; }

    public CZ_REQUEST_ACTION() : base(PacketHeader.CZ_REQUEST_ACTION, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        TargetId = reader.ReadInt32();
        Action = reader.ReadByte();
    }

    public static CZ_REQUEST_ACTION Create(BinaryReader reader)
    {
        var packet = new CZ_REQUEST_ACTION();
        packet.Read(reader);
        return packet;
    }
}
