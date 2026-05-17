using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_LONGLONGPAR_CHANGE</c> (0x0ACB) — the 64-bit
/// value variant used for modern SP_BASEEXP/SP_JOBEXP at
/// PACKETVER ≥ 20170830. Fixed 12 bytes:
/// header (2) + varId (2) + value (8).
/// </summary>
public sealed class ZcLongLongParChangeDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_LONGLONGPAR_CHANGE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var varId = r.ReadUInt16();
        var value = r.ReadInt64();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("VarId", SpId.Format(varId)),
            new("Value", value),
        });
    }
}
