namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena mail-system notification at PACKETVER ≥ 20170419 (rodex
/// related). Fixed 5 bytes: header (2) + 3-byte body. Captured emits
/// two copies on login — likely paired with the unread-mail check.
/// Real layout TBD; stub for replay-parser purposes.
/// </summary>
public class ZC_MAIL_NEW_NOTIFY : OutgoingPacket
{
    private const int SIZE = 5;

    public byte[] Body { get; init; } = new byte[3];

    public ZC_MAIL_NEW_NOTIFY() : base(PacketHeader.ZC_MAIL_NEW_NOTIFY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        var pad = new byte[3];
        Array.Copy(Body, pad, Math.Min(Body.Length, 3));
        writer.Write(pad);
    }
}
