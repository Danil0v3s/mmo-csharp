using System.Reflection;
using Core.Server.Packets;

namespace Tools.PacketReplay.Tokens;

/// <summary>
/// Correlates session tokens (AID, login_id1, login_id2, etc.) between the
/// captured expected response and our live server's actual response, then
/// rewrites future client→server bytes so stale captured tokens become
/// the values our server is actually using.
///
/// Without this, any rAthena capture is fatally bound to the AID + random
/// session tokens that particular rAthena instance assigned; replay against
/// our server (which generates its own tokens) would always fail the next
/// auth gate.
///
/// Rewriting strategy is byte-sequence find/replace. Tokens are 4-byte
/// little-endian uints; collisions with unrelated bytes are statistically
/// unlikely (~1 in 4 billion for a uniformly-random token) but not
/// impossible — the report surfaces every substitution that fires so a
/// false positive is auditable.
/// </summary>
public sealed class TokenRewriter
{
    private readonly PacketFramer _framer;
    private readonly Dictionary<PacketHeader, ITokenExtractor> _extractors;
    private readonly List<TokenSubstitution> _subs = new();

    public TokenRewriter(IPacketSizeRegistry sizes)
    {
        _framer = new PacketFramer(sizes);
        _extractors = DiscoverExtractors();
    }

    /// <summary>Substitutions registered so far. Useful for reports.</summary>
    public IReadOnlyList<TokenSubstitution> Substitutions => _subs;

    private static Dictionary<PacketHeader, ITokenExtractor> DiscoverExtractors()
    {
        var byHeader = new Dictionary<PacketHeader, ITokenExtractor>();
        foreach (var type in typeof(TokenRewriter).Assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;
            if (!typeof(ITokenExtractor).IsAssignableFrom(type)) continue;
            if (type.GetConstructor(Type.EmptyTypes) == null) continue;
            var instance = (ITokenExtractor)Activator.CreateInstance(type)!;
            byHeader[instance.Header] = instance;
        }
        return byHeader;
    }

    /// <summary>
    /// After receiving a chunk, frame both expected (capture) and actual
    /// (server) byte streams, walk them in parallel, and let each matching
    /// packet's extractor record substitutions. Frame counts may diverge
    /// (server may send extra or fewer packets) — we walk only as far as
    /// both have a packet at the same index.
    /// </summary>
    public void OnReceived(byte[] expectedChunk, byte[] actualChunk)
    {
        if (_extractors.Count == 0) return;
        var exp = _framer.Frame(expectedChunk);
        var act = _framer.Frame(actualChunk);
        var n = Math.Min(exp.Packets.Count, act.Packets.Count);
        for (var i = 0; i < n; i++)
        {
            var e = exp.Packets[i];
            var a = act.Packets[i];
            if (e.Header != a.Header) continue;
            if (!_extractors.TryGetValue(e.Header, out var ext)) continue;
            foreach (var sub in ext.Extract(e.Body, a.Body))
            {
                if (sub.From.Length == sub.To.Length && !sub.From.AsSpan().SequenceEqual(sub.To))
                {
                    _subs.Add(sub);
                }
            }
        }
    }

    /// <summary>
    /// Apply every registered substitution to <paramref name="input"/>.
    /// Returns a new array; the original is untouched. Multiple
    /// occurrences of the same captured token are all replaced.
    /// </summary>
    public byte[] Apply(byte[] input)
    {
        if (_subs.Count == 0) return input;
        var output = (byte[])input.Clone();
        foreach (var sub in _subs)
        {
            var from = sub.From;
            for (var i = 0; i <= output.Length - from.Length; i++)
            {
                if (output.AsSpan(i, from.Length).SequenceEqual(from))
                {
                    sub.To.AsSpan().CopyTo(output.AsSpan(i, from.Length));
                    i += from.Length - 1;
                }
            }
        }
        return output;
    }
}
