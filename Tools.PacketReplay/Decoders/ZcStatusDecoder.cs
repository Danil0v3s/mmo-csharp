using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>ZC_STATUS</c> (0x00BD) — the big initial-status
/// snapshot from <c>clif_initialstatus</c>. Fixed 44 bytes; all 26+
/// fields decoded so the comparer can report any derived-stat formula
/// divergence by name.
/// </summary>
public sealed class ZcStatusDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.ZC_STATUS;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16();

        return new DecodedPacket(Header, new List<DecodedField>
        {
            new("StatusPoint", r.ReadUInt16()),
            new("Str", r.ReadByte()),
            new("StandardStr", r.ReadByte()),
            new("Agi", r.ReadByte()),
            new("StandardAgi", r.ReadByte()),
            new("Vit", r.ReadByte()),
            new("StandardVit", r.ReadByte()),
            new("Int", r.ReadByte()),
            new("StandardInt", r.ReadByte()),
            new("Dex", r.ReadByte()),
            new("StandardDex", r.ReadByte()),
            new("Luk", r.ReadByte()),
            new("StandardLuk", r.ReadByte()),
            new("AttPower", r.ReadInt16()),
            new("RefiningPower", r.ReadInt16()),
            new("MaxMattPower", r.ReadInt16()),
            new("MinMattPower", r.ReadInt16()),
            new("ItemDefPower", r.ReadInt16()),
            new("PlusDefPower", r.ReadInt16()),
            new("MdefPower", r.ReadInt16()),
            new("PlusMdefPower", r.ReadInt16()),
            new("Hit", r.ReadInt16()),
            new("Flee", r.ReadInt16()),
            new("Flee2", r.ReadInt16()),
            new("Crit", r.ReadInt16()),
            new("Aspd", r.ReadInt16()),
            new("PlusAspd", r.ReadInt16()),
        });
    }
}
