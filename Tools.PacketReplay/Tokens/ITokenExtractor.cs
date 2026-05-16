using Core.Server.Packets;

namespace Tools.PacketReplay.Tokens;

/// <summary>
/// Pulls per-session tokens (account_id, login_id1, login_id2, char_id,
/// etc.) out of a server response so the replay framework can rewrite
/// stale captured values in subsequent <c>S|</c> sends to match what our
/// server actually returned.
///
/// One extractor per packet type that introduces a new correlated value.
/// Discovered reflectively by <see cref="TokenRewriter"/>.
/// </summary>
public interface ITokenExtractor
{
    PacketHeader Header { get; }

    /// <summary>
    /// Given the framed packet bytes from the capture (<paramref name="expected"/>)
    /// and from the live server (<paramref name="actual"/>), yield each
    /// <c>(captured-bytes, actual-bytes)</c> pair the framework should
    /// substitute. Implementations consume the full packet bytes including
    /// the wire-level header (and length, for variable-length packets).
    /// </summary>
    IEnumerable<TokenSubstitution> Extract(byte[] expected, byte[] actual);
}

/// <summary>
/// One byte-sequence substitution. The framework finds <see cref="From"/>
/// in subsequent client→server bytes and rewrites it to <see cref="To"/>.
/// Both must be the same length.
/// </summary>
public sealed record TokenSubstitution(string Name, byte[] From, byte[] To);
