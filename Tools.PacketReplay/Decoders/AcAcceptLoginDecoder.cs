using System.Text;
using Core.Server.Packets;

namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Decoder for <c>AC_ACCEPT_LOGIN</c> (0x0AC4) — the login-success packet,
/// rAthena <c>clif_AC_ACCEPT_LOGIN</c>. Layout (PACKETVER ≥ 20170315,
/// which our 20211103 satisfies):
///
/// <code>
///   int16 packetType        2  ← header (skipped in decode)
///   int16 packetLength      2  ← variable-length prefix (skipped)
///   uint32 login_id1        4  ← TOLERANT (per-session random token)
///   uint32 AID              4
///   uint32 login_id2        4  ← TOLERANT (per-session random token)
///   uint32 last_ip          4  ← TOLERANT (host-dependent)
///   char   last_login[26]   26 ← TOLERANT (timestamp text)
///   uint8  sex              1
///   char   token[17]        17 ← TOLERANT (web_auth_token, varies)
///   AC_ACCEPT_LOGIN_sub[]      ← one per registered char server (160B each)
/// </code>
/// </summary>
public sealed class AcAcceptLoginDecoder : IPacketDecoder
{
    public PacketHeader Header => PacketHeader.AC_ACCEPT_LOGIN;

    public DecodedPacket Decode(byte[] packetBytes)
    {
        using var ms = new MemoryStream(packetBytes);
        using var r = new BinaryReader(ms);
        r.ReadInt16(); // packetType
        var packetLength = r.ReadInt16();

        var fields = new List<DecodedField>
        {
            new("LoginId1", r.ReadUInt32(), Tolerant: true),
            new("AID", r.ReadUInt32()),
            new("LoginId2", r.ReadUInt32(), Tolerant: true),
            new("LastIp", r.ReadUInt32(), Tolerant: true),
            new("LastLogin", ReadFixedString(r, 26), Tolerant: true),
            new("Sex", r.ReadByte()),
            new("Token", ReadFixedString(r, 17), Tolerant: true),
        };

        // Char-server list fills the rest of the payload (160 bytes each).
        var subSize = 4 + 2 + 20 + 2 + 2 + 2 + 128;
        var serverIndex = 0;
        while (ms.Position + subSize <= packetLength)
        {
            var ip = r.ReadUInt32();
            var port = r.ReadUInt16();
            var name = ReadFixedString(r, 20);
            var users = r.ReadUInt16();
            var type = r.ReadUInt16();
            var newFlag = r.ReadUInt16();
            var unknown = r.ReadBytes(128);

            fields.Add(new($"CharServers[{serverIndex}].Ip", ip, Tolerant: true));
            fields.Add(new($"CharServers[{serverIndex}].Port", port));
            fields.Add(new($"CharServers[{serverIndex}].Name", name));
            fields.Add(new($"CharServers[{serverIndex}].Users", users, Tolerant: true));
            fields.Add(new($"CharServers[{serverIndex}].Type", type));
            fields.Add(new($"CharServers[{serverIndex}].New", newFlag));
            fields.Add(new($"CharServers[{serverIndex}].Unknown.Length", unknown.Length));
            serverIndex++;
        }

        return new DecodedPacket(Header, fields);
    }

    private static string ReadFixedString(BinaryReader r, int length)
    {
        var bytes = r.ReadBytes(length);
        var nullAt = Array.IndexOf(bytes, (byte)0);
        var len = nullAt < 0 ? bytes.Length : nullAt;
        return Encoding.ASCII.GetString(bytes, 0, len);
    }
}
