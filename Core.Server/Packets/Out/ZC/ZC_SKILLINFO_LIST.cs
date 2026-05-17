namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_skillinfoblock</c> ([clif.cpp:5697]). The player's
/// known-skills snapshot sent on first map login. Variable length;
/// per-skill entries are 37 bytes each (id + type + lv + sp + range +
/// name + upgradable). For replay parsing the registry only needs the
/// variable-length marker; full serialization lands with the skill
/// system.
/// </summary>
public class ZC_SKILLINFO_LIST : OutgoingPacket
{
    public byte[] Body { get; init; } = Array.Empty<byte>();

    public ZC_SKILLINFO_LIST() : base(PacketHeader.ZC_SKILLINFO_LIST, -1) { }

    public override int GetSize() => sizeof(short) + sizeof(short) + Body.Length;

    public override void Write(BinaryWriter writer) => writer.Write(Body);
}
