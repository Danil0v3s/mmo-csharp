using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_PAR_CHANGE</c> (0x00B0). Fixed 8 bytes:
/// header (2) + varId (2) + value (4). VarId is rendered as
/// "SP_NAME(id)" via <see cref="SpId.Format"/>.
///
/// Every field is strict — the value comes from a deterministic
/// projection of saved character data through rAthena's renewal
/// stat formulas, so any divergence is a formula bug to surface.
/// </summary>
public sealed class ZcParChangeDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_PAR_CHANGE;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType
        var varId = r.ReadUInt16();
        var value = r.ReadInt32();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("VarId", SpId.Format(varId)),
            new("Value", value),
        });
    }
}
