using Core.Server.Packets;

namespace Tools.PacketReplay.Tokens;

/// <summary>
/// Extracts the three correlated fields a successful login emits:
/// <c>login_id1</c>, <c>AID</c>, <c>login_id2</c> — each a 4-byte
/// little-endian uint at fixed offsets in <c>AC_ACCEPT_LOGIN</c>'s body.
///
/// Layout (PACKETVER ≥ 20170315):
/// <code>
///   offset  field
///        0  packetType  (2 bytes)
///        2  packetLen   (2 bytes)
///        4  login_id1   (4 bytes) ← rewrite target
///        8  AID         (4 bytes) ← rewrite target
///       12  login_id2   (4 bytes) ← rewrite target
/// </code>
/// </summary>
public sealed class AcAcceptLoginTokens : ITokenExtractor
{
    public PacketHeader Header => PacketHeader.AC_ACCEPT_LOGIN;

    public IEnumerable<TokenSubstitution> Extract(byte[] expected, byte[] actual)
    {
        if (expected.Length < 16 || actual.Length < 16) yield break;
        yield return new("login_id1", expected[4..8],  actual[4..8]);
        yield return new("AID",       expected[8..12], actual[8..12]);
        yield return new("login_id2", expected[12..16], actual[12..16]);
    }
}
