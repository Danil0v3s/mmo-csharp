using Core.Server.Packets;

namespace Tools.PacketReplay.Tokens;

/// <summary>
/// Pulls <c>char_id</c> out of <c>HC_SEND_MAP_DATA</c> (the map-handoff
/// packet) so the next <c>CZ_WANT_TO_CONNECTION</c> the capture sends to
/// the map server can be rewritten to carry our locally-assigned char_id
/// instead of the captured one. Without this the map server's auth gate
/// rejects the connection (the char_id doesn't match its pending-auth
/// ticket) and ZC_AID never goes out.
///
/// Layout (HC_SEND_MAP_DATA is fixed 156 bytes):
/// <code>
///   offset  field
///        0  packetType  (2 bytes)
///        2  charId      (4 bytes) ← rewrite target
///        6  mapName[16]
///       22  ip          (4 bytes)
///       26  port        (2 bytes)
///       28  domain[128]
/// </code>
/// </summary>
public sealed class HcSendMapDataTokens : ITokenExtractor
{
    public PacketHeader Header => PacketHeader.HC_SEND_MAP_DATA;

    public IEnumerable<TokenSubstitution> Extract(byte[] expected, byte[] actual)
    {
        if (expected.Length < 6 || actual.Length < 6) yield break;
        yield return new("char_id", expected[2..6], actual[2..6]);
    }
}
