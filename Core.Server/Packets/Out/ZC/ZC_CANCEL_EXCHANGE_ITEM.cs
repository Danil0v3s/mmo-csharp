namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_tradecancelled</c> (clif.cpp:4827). Wire (0x00ee):
/// header only — 2 bytes.
/// </summary>
public class ZC_CANCEL_EXCHANGE_ITEM : OutgoingPacket
{
    private const int SIZE = 2;

    public ZC_CANCEL_EXCHANGE_ITEM() : base(PacketHeader.ZC_CANCEL_EXCHANGE_ITEM, SIZE) { }

    public override void Write(BinaryWriter writer) { /* no body */ }
}
