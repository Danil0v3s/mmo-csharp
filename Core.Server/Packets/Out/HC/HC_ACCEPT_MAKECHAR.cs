namespace Core.Server.Packets.Out.HC;

/// <summary>
/// "Character created successfully — here's the new character row."
/// rAthena <c>clif_hc_makechar_accept</c>. Fixed-length:
///   packetType (2) + CharacterInfo (175) = 177 bytes.
///
/// Despite carrying a variable-shape payload conceptually, on the wire
/// this packet is fixed at exactly one <see cref="CharacterInfo"/> block
/// (175 bytes for the current PACKETVER). Declaring it fixed lets the
/// packet-size registry frame it correctly without the
/// <c>HasPacketLength = false</c> + <c>Size = -1</c> combination, which
/// previously confused the replay framer (it tried to read bytes 2-3 as
/// a length prefix that doesn't exist).
/// </summary>
public class HC_ACCEPT_MAKECHAR : OutgoingPacket
{
    private const int SIZE = sizeof(short) + global::Core.Server.Packets.CharacterInfo.SerializedSize; // 2 + 175 = 177

    public byte[] CharData { get; init; } = Array.Empty<byte>();

    public HC_ACCEPT_MAKECHAR() : base(PacketHeader.HC_ACCEPT_MAKECHAR, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        global::Core.Server.Packets.CharacterInfo.ValidateSingleSize(CharData.Length, nameof(CharData));
        writer.Write(CharData);
    }
}
