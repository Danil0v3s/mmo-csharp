namespace Core.Server.Packets.Out.HC;

public class HC_ACCEPT_ENTER2 : OutgoingPacket
{
    private const int ExtensionLength = 20;
    private const int PacketSize = sizeof(short) + sizeof(short) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(byte) + ExtensionLength;

    public byte Normal { get; init; }
    public byte Premium { get; init; }
    public byte Billing { get; init; }
    public byte Producible { get; init; }
    public byte Total { get; init; }
    public string Extension { get; init; } = string.Empty;

    public HC_ACCEPT_ENTER2() : base(PacketHeader.HC_ACCEPT_ENTER2, -1) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Normal);
        writer.Write(Premium);
        writer.Write(Billing);
        writer.Write(Producible);
        writer.Write(Total);
        writer.WriteFixedString(Extension, ExtensionLength);
    }

    public override int GetSize() => PacketSize;
}
