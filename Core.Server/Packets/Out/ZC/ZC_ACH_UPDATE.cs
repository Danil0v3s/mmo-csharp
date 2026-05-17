namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_achievement_update</c> ([clif.cpp:21815]). Single
/// achievement progress notification. Fixed 66 bytes; the field shape
/// (rAthena <c>struct PACKET_ZC_ACH_UPDATE</c>) is large and gameplay-
/// specific — stub here as opaque bytes until [achievement.md] lands.
/// </summary>
public class ZC_ACH_UPDATE : OutgoingPacket
{
    private const int SIZE = 66;

    public byte[] Body { get; init; } = new byte[SIZE - sizeof(short)];

    public ZC_ACH_UPDATE() : base(PacketHeader.ZC_ACH_UPDATE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        var pad = new byte[SIZE - sizeof(short)];
        Array.Copy(Body, pad, Math.Min(Body.Length, pad.Length));
        writer.Write(pad);
    }
}
