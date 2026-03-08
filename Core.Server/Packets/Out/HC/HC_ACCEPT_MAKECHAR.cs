namespace Core.Server.Packets.Out.HC;

public class HC_ACCEPT_MAKECHAR : OutgoingPacket
{
    public byte[] CharData { get; init; } = Array.Empty<byte>();

    public HC_ACCEPT_MAKECHAR() : base(PacketHeader.HC_ACCEPT_MAKECHAR, -1) { }

    public override void Write(BinaryWriter writer)
    {
        global::Core.Server.Packets.CharacterInfo.ValidateSingleSize(CharData.Length, nameof(CharData));
        writer.Write((short)Header);
        writer.Write(CharData);
    }

    public override int GetSize()
    {
        global::Core.Server.Packets.CharacterInfo.ValidateSingleSize(CharData.Length, nameof(CharData));
        // PACKET_HC_ACCEPT_MAKECHAR has no packetLength field.
        return sizeof(short) + CharData.Length;
    }
}
