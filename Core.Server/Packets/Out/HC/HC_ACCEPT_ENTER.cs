namespace Core.Server.Packets.Out.HC;

public class HC_ACCEPT_ENTER : OutgoingPacket
{
    private const int ExtensionLength = 20;

    public byte Total { get; init; }
    public byte PremiumStart { get; init; }
    public byte PremiumEnd { get; init; }
    public string Extension { get; init; } = string.Empty;
    public byte[] CharacterData { get; init; } = Array.Empty<byte>();

    public HC_ACCEPT_ENTER() : base(PacketHeader.HC_ACCEPT_ENTER, -1) { }

    public override void Write(BinaryWriter writer)
    {
        global::Core.Server.Packets.CharacterInfo.ValidateBlockSize(CharacterData.Length, nameof(CharacterData));
        writer.Write(Total);
        writer.Write(PremiumStart);
        writer.Write(PremiumEnd);
        writer.WriteFixedString(Extension, ExtensionLength);
        writer.Write(CharacterData);
    }

    public override int GetSize()
    {
        global::Core.Server.Packets.CharacterInfo.ValidateBlockSize(CharacterData.Length, nameof(CharacterData));
        return sizeof(short) + sizeof(short) + sizeof(byte) + sizeof(byte) + sizeof(byte) + ExtensionLength + CharacterData.Length;
    }
}
