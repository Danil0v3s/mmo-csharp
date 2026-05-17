using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_LONGPAR_CHANGE</c> (0x00B1). Same shape as
/// <see cref="ZcParChangeDecoder"/> but routed to the legacy long
/// path (zeny / pre-PACKETVER 20170830 exp). Fixed 8 bytes.
/// </summary>
public sealed class ZcLongParChangeDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_LONGPAR_CHANGE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var varId = r.ReadUInt16();
        var value = r.ReadInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("VarId", SpId.Format(varId)),
            new("Value", value),
        });
    }
}
