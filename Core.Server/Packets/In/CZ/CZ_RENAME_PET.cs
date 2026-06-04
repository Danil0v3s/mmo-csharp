using System.Text;

namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Request to rename the active pet. rAthena <c>clif_parse_ChangePetName</c> (clif.cpp, 0x01a5).
/// Fixed 26 bytes: <c>01a5 &lt;name&gt;.24B</c> (NUL-padded ASCII).
/// </summary>
public class CZ_RENAME_PET : IncomingPacket
{
    private const int NameLength = 24;
    private const int SIZE = 2 + NameLength;

    public string Name { get; private set; } = string.Empty;

    public CZ_RENAME_PET() : base(PacketHeader.CZ_RENAME_PET, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        var buf = reader.ReadBytes(NameLength);
        var end = Array.IndexOf(buf, (byte)0);
        if (end < 0) end = NameLength;
        Name = Encoding.ASCII.GetString(buf, 0, end);
    }

    public static CZ_RENAME_PET Create(BinaryReader reader)
    {
        var packet = new CZ_RENAME_PET();
        packet.Read(reader);
        return packet;
    }
}
