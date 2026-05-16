using System.Net.Sockets;
using Tools.PacketReplay.Tokens;

namespace Tools.PacketReplay;

/// <summary>
/// Drives a packet capture against a running server stack. Each event
/// carries the port the capture talked to; when that port changes the
/// runner closes the current TCP connection and opens a new one to the
/// local equivalent. Port mapping is supplied at construction so the same
/// fixture can target either an upstream rAthena (5121 = map) or our
/// local map server (5191).
///
/// Capture order:
///   For Send events: write bytes to the current connection.
///   For Recv events: read exactly <c>event.Bytes.Length</c> bytes off the
///   socket and stash them as the "actual" chunk; the comparer pairs
///   actual vs expected by source line.
/// </summary>
public sealed class ReplaySession : IAsyncDisposable
{
    private readonly string _host;
    private readonly IReadOnlyDictionary<int, int> _portRemap;
    private readonly TimeSpan _readTimeout;
    private readonly TimeSpan _quietPeriod;
    private readonly TokenRewriter? _rewriter;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _currentPort;

    public ReplaySession(
        string host,
        IReadOnlyDictionary<int, int> portRemap,
        TimeSpan readTimeout,
        TokenRewriter? rewriter = null,
        TimeSpan? quietPeriod = null)
    {
        _host = host;
        _portRemap = portRemap;
        _readTimeout = readTimeout;
        // After this gap of socket silence we stop waiting for the rest of
        // the expected chunk and let the comparer surface a length diff.
        // Keeps the report honest when our server sends fewer bytes than
        // the capture expected.
        _quietPeriod = quietPeriod ?? TimeSpan.FromMilliseconds(250);
        _rewriter = rewriter;
    }

    public async Task<ReplayCapture> ReplayAsync(PacketLogFile file, CancellationToken ct = default)
    {
        var expected = new List<ReplayChunk>();
        var actual = new List<ReplayChunk>();
        string? earlyClose = null;

        foreach (var ev in file.Events)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (ev.Port != _currentPort)
                {
                    await SwitchConnectionAsync(ev.Port, ct);
                }

                if (ev.Direction == ReplayDirection.Send)
                {
                    // Rewrite stale captured tokens (AID, login_id1/2, …)
                    // to the values our server actually returned so that
                    // every subsequent auth gate validates against live
                    // state, not the originator's session.
                    var toSend = _rewriter?.Apply(ev.Bytes) ?? ev.Bytes;
                    await _stream!.WriteAsync(toSend.AsMemory(), ct);
                    await _stream.FlushAsync(ct);
                }
                else
                {
                    var actualBytes = await ReadExactAsync(ev.Bytes.Length, ct);
                    expected.Add(new ReplayChunk(ev.SourceLine, ev.Port, ev.Bytes));
                    actual.Add(new ReplayChunk(ev.SourceLine, ev.Port, actualBytes));
                    // Surface ONLY connection drops as "early close" —
                    // shorter-than-expected reads (server returned less
                    // than the capture) are real divergences but the
                    // session keeps going so the comparer can render the
                    // length diff and any subsequent chunks.
                    if (actualBytes.Length == 0 && ev.Bytes.Length > 0)
                    {
                        earlyClose = $"server closed connection on port {ev.Port} at capture line {ev.SourceLine} "
                                   + $"(expected {ev.Bytes.Length}B, server sent nothing)";
                        break;
                    }
                    // Let the rewriter sniff token-bearing packets so the
                    // next S| chunk can be rewritten in place.
                    _rewriter?.OnReceived(ev.Bytes, actualBytes);
                }
            }
            catch (IOException ex) when (IsConnectionDrop(ex))
            {
                earlyClose = $"server dropped connection on port {ev.Port} at capture line {ev.SourceLine} "
                           + $"(during {ev.Direction}): {ex.GetBaseException().Message}";
                break;
            }
        }

        return new ReplayCapture(expected, actual, earlyClose);
    }

    private static bool IsConnectionDrop(IOException ex)
    {
        return ex.InnerException is SocketException
            || ex.Message.Contains("Broken pipe", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("reset", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SwitchConnectionAsync(int capturePort, CancellationToken ct)
    {
        if (_stream != null) await _stream.DisposeAsync();
        _client?.Dispose();

        if (!_portRemap.TryGetValue(capturePort, out var localPort))
        {
            throw new InvalidOperationException(
                $"No port mapping for capture port {capturePort}. "
                + $"Configured maps: {string.Join(", ", _portRemap.Select(kv => $"{kv.Key}→{kv.Value}"))}");
        }

        _client = new TcpClient();
        await _client.ConnectAsync(_host, localPort, ct);
        _stream = _client.GetStream();
        _stream.ReadTimeout = (int)_readTimeout.TotalMilliseconds;
        _currentPort = capturePort;
    }

    /// <summary>
    /// Adaptive read: drain up to <paramref name="count"/> bytes from the
    /// stream. Two-phase deadlines so we don't either block forever or bail
    /// before the server has a chance to respond:
    ///
    ///   Phase 1 (before any bytes arrive): wait up to <c>_readTimeout</c>.
    ///   Phase 2 (after the first byte):    wait up to <c>_quietPeriod</c>
    ///                                      between successive reads.
    ///
    /// This lets the comparer surface "got fewer than expected" as a regular
    /// length-mismatch diff once the server's response goes idle, instead of
    /// blocking on the full ReadTimeout every time our response is shorter
    /// than the captured one.
    ///
    /// Three terminal states:
    ///   1. We read the full <paramref name="count"/> bytes — return them.
    ///   2. The server closed the connection — return what we have.
    ///   3. Phase 1 timed out (no response at all) OR phase 2 went quiet —
    ///      return what we have, comparer reports the length diff.
    /// </summary>
    private async Task<byte[]> ReadExactAsync(int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var read = 0;
        while (read < count)
        {
            var deadline = read == 0 ? _readTimeout : _quietPeriod;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(deadline);
            int n;
            try
            {
                n = await _stream!.ReadAsync(buffer.AsMemory(read, count - read), cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Either no bytes ever arrived (phase 1) or stream went
                // quiet after a partial response (phase 2). Either way,
                // bail with what we have; comparer surfaces the diff.
                Array.Resize(ref buffer, read);
                return buffer;
            }
            if (n == 0)
            {
                // Connection actually closed (FIN). Same partial return.
                Array.Resize(ref buffer, read);
                return buffer;
            }
            read += n;
        }
        return buffer;
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null) await _stream.DisposeAsync();
        _client?.Dispose();
    }
}

public sealed record ReplayChunk(int SourceLine, int Port, byte[] Bytes);

public sealed record ReplayCapture(
    IReadOnlyList<ReplayChunk> Expected,
    IReadOnlyList<ReplayChunk> Actual,
    string? EarlyCloseReason = null);
