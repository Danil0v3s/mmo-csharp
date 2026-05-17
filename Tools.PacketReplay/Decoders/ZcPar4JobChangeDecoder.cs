using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_PAR_4JOB_CHANGE</c> (0x0B25). Same shape as
/// <see cref="ZcCoupleStatusDecoder"/> but with a 4-byte varId for
/// the 4-job UI. Fixed 14 bytes.
/// </summary>
public sealed class ZcPar4JobChangeDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_PAR_4JOB_CHANGE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();
        var varId = r.ReadUInt32();
        var baseStatus = r.ReadInt32();
        var plusStatus = r.ReadInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("VarId", SpId.Format(varId)),
            new("Base", baseStatus),
            new("Plus", plusStatus),
        });
    }
}
