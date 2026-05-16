using System.Text;
using Core.Server.Packets;
using Tools.PacketReplay.Decoders;

namespace Tools.PacketReplay;

/// <summary>
/// Compares the expected and actual streams from a <see cref="ReplayCapture"/>.
/// Strategy:
///   1. Per <c>R|</c> chunk, frame both byte buffers into wire packets using
///      the registered packet-size table. Unknown packet ids are surfaced
///      directly (no attempt to interpret further).
///   2. For each pair of (expected, actual) packets:
///      - Headers must match (same packet id).
///      - Bodies are compared byte-for-byte.
///   3. Any drift in packet count, header order, or byte content shows up
///      in the resulting <see cref="ComparisonReport"/>.
///
/// This is structural to the extent that we identify packet boundaries and
/// per-packet headers; field-level introspection (e.g. tolerating a server
/// tick) needs masks on top of this — wired in when fixtures call for it.
/// </summary>
public sealed class PacketComparer
{
    private readonly PacketFramer _framer;
    private readonly DecoderRegistry _decoders;

    public PacketComparer(IPacketSizeRegistry sizes, DecoderRegistry? decoders = null)
    {
        _framer = new PacketFramer(sizes);
        _decoders = decoders ?? new DecoderRegistry();
    }

    public ComparisonReport Compare(ReplayCapture capture)
    {
        var diffs = new List<ChunkDiff>();
        var earlyClose = capture.EarlyCloseReason;
        for (var i = 0; i < capture.Expected.Count; i++)
        {
            var expectedChunk = capture.Expected[i];
            var actualChunk = i < capture.Actual.Count
                ? capture.Actual[i]
                : new ReplayChunk(expectedChunk.SourceLine, expectedChunk.Port, Array.Empty<byte>());

            var expFrames = FrameWithPreludeFallback(expectedChunk.Bytes);
            var actFrames = FrameWithPreludeFallback(actualChunk.Bytes);

            diffs.Add(new ChunkDiff(
                SourceLine: expectedChunk.SourceLine,
                Port: expectedChunk.Port,
                Expected: expFrames,
                Actual: actFrames,
                PacketDiffs: ComparePackets(expFrames.Packets, actFrames.Packets)));
        }
        return new ComparisonReport(diffs, earlyClose);
    }

    /// <summary>
    /// Try framing without a prelude first; if the first 2 bytes don't map
    /// to a registered packet header AND the buffer is at least 6 bytes,
    /// retry with a 4-byte prelude. This handles the char server's
    /// "bare account_id prefix" before the first packet on a fresh
    /// connection (rAthena's clif_charlistnotify and similar).
    /// </summary>
    private FramingResult FrameWithPreludeFallback(byte[] bytes)
    {
        var primary = _framer.Frame(bytes, preludeLength: 0);
        if (primary.UnknownPackets.Count > 0 && primary.UnknownPackets[0].Offset == 0 && bytes.Length >= 6)
        {
            var fallback = _framer.Frame(bytes, preludeLength: 4);
            if (fallback.UnknownPackets.Count == 0 || fallback.Packets.Count > primary.Packets.Count)
            {
                return fallback;
            }
        }
        return primary;
    }

    private IReadOnlyList<PacketDiff> ComparePackets(
        IReadOnlyList<FramedPacket> expected, IReadOnlyList<FramedPacket> actual)
    {
        var diffs = new List<PacketDiff>();
        var max = Math.Max(expected.Count, actual.Count);
        for (var i = 0; i < max; i++)
        {
            var exp = i < expected.Count ? expected[i] : null;
            var act = i < actual.Count ? actual[i] : null;

            if (exp == null)
            {
                diffs.Add(new PacketDiff(i, PacketDiffKind.Extra, exp, act, ByteDiffs: Array.Empty<ByteDiff>(), FieldDiffs: Array.Empty<FieldDiff>()));
                continue;
            }
            if (act == null)
            {
                diffs.Add(new PacketDiff(i, PacketDiffKind.Missing, exp, act, ByteDiffs: Array.Empty<ByteDiff>(), FieldDiffs: Array.Empty<FieldDiff>()));
                continue;
            }
            if (exp.Header != act.Header)
            {
                diffs.Add(new PacketDiff(i, PacketDiffKind.HeaderMismatch, exp, act, ByteDiffs: Array.Empty<ByteDiff>(), FieldDiffs: Array.Empty<FieldDiff>()));
                continue;
            }

            // Prefer the structural decoder when one is registered; that
            // gives us per-field tolerance for stochastic values (random
            // session tokens, server tick, etc.).
            if (_decoders.TryGet(exp.Header, out var decoder))
            {
                var fieldDiffs = DiffFields(decoder, exp.Body, act.Body);
                if (fieldDiffs.Count > 0)
                {
                    diffs.Add(new PacketDiff(i, PacketDiffKind.FieldMismatch, exp, act,
                        ByteDiffs: Array.Empty<ByteDiff>(), FieldDiffs: fieldDiffs));
                }
                continue;
            }

            var byteDiffs = DiffBytes(exp.Body, act.Body);
            if (byteDiffs.Count > 0)
            {
                diffs.Add(new PacketDiff(i, PacketDiffKind.BodyMismatch, exp, act, byteDiffs, FieldDiffs: Array.Empty<FieldDiff>()));
            }
        }
        return diffs;
    }

    private static IReadOnlyList<FieldDiff> DiffFields(IPacketDecoder decoder, byte[] expected, byte[] actual)
    {
        DecodedPacket exp, act;
        try { exp = decoder.Decode(expected); }
        catch (Exception ex) { return new[] { new FieldDiff("(decoder)", "(threw on expected)", ex.Message, false) }; }
        try { act = decoder.Decode(actual); }
        catch (Exception ex) { return new[] { new FieldDiff("(decoder)", "(threw on actual)", ex.Message, false) }; }

        var byName = exp.Fields.ToDictionary(f => f.Name);
        var diffs = new List<FieldDiff>();
        foreach (var actualField in act.Fields)
        {
            if (!byName.TryGetValue(actualField.Name, out var expectedField))
            {
                diffs.Add(new FieldDiff(actualField.Name, "(missing)", actualField.Value, Tolerant: false));
                continue;
            }
            if (!Equals(expectedField.Value, actualField.Value))
            {
                diffs.Add(new FieldDiff(
                    Name: actualField.Name,
                    Expected: expectedField.Value,
                    Actual: actualField.Value,
                    Tolerant: expectedField.Tolerant));
            }
            byName.Remove(actualField.Name);
        }
        foreach (var leftover in byName.Values)
        {
            diffs.Add(new FieldDiff(leftover.Name, leftover.Value, "(missing in actual)", Tolerant: leftover.Tolerant));
        }
        // Strip tolerant-only diffs from the failure surface; surface only the
        // ones that should drive a test red.
        return diffs.Where(d => !d.Tolerant).ToList();
    }

    private static IReadOnlyList<ByteDiff> DiffBytes(byte[] expected, byte[] actual)
    {
        if (expected.Length != actual.Length)
        {
            // Length mismatch — single synthetic diff capturing the overall shape.
            return new[]
            {
                new ByteDiff(Offset: 0, ExpectedByte: null, ActualByte: null,
                    Note: $"length expected={expected.Length} actual={actual.Length}")
            };
        }
        var diffs = new List<ByteDiff>();
        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                diffs.Add(new ByteDiff(i, expected[i], actual[i], null));
            }
        }
        return diffs;
    }
}

public sealed record ComparisonReport(IReadOnlyList<ChunkDiff> Chunks, string? EarlyCloseReason = null)
{
    public bool Passed => EarlyCloseReason == null && Chunks.All(c => c.IsClean);

    public string Render()
    {
        var sb = new StringBuilder();
        if (EarlyCloseReason != null)
        {
            sb.AppendLine($"!! EARLY CLOSE: {EarlyCloseReason}");
            sb.AppendLine($"   (chunks captured before close: {Chunks.Count})");
        }
        foreach (var chunk in Chunks)
        {
            sb.AppendLine($"--- R| line {chunk.SourceLine} (port {chunk.Port}) ---");
            if (chunk.Expected.UnknownPackets.Count > 0 || chunk.Actual.UnknownPackets.Count > 0)
            {
                foreach (var u in chunk.Expected.UnknownPackets)
                    sb.AppendLine($"  EXPECTED: unknown packet 0x{(short)u.Header:X4} at offset {u.Offset} (+{u.Tail.Length} bytes)");
                foreach (var u in chunk.Actual.UnknownPackets)
                    sb.AppendLine($"  ACTUAL:   unknown packet 0x{(short)u.Header:X4} at offset {u.Offset} (+{u.Tail.Length} bytes)");
            }
            if (chunk.PacketDiffs.Count == 0
                && chunk.Expected.UnknownPackets.Count == 0
                && chunk.Actual.UnknownPackets.Count == 0
                && chunk.Expected.TrailingBytes == 0
                && chunk.Actual.TrailingBytes == 0)
            {
                sb.AppendLine($"  ✓ {chunk.Expected.Packets.Count} packet(s) match");
                continue;
            }
            foreach (var diff in chunk.PacketDiffs)
            {
                sb.AppendLine(RenderPacketDiff(diff));
            }
            if (chunk.Expected.TrailingBytes != 0 || chunk.Actual.TrailingBytes != 0)
            {
                sb.AppendLine($"  trailing bytes: expected={chunk.Expected.TrailingBytes} actual={chunk.Actual.TrailingBytes}");
            }
        }
        return sb.ToString();
    }

    private static string FormatValue(object? v) => v switch
    {
        null => "(null)",
        string s => $"\"{s}\"",
        byte[] b => "[" + string.Join(" ", b.Take(16).Select(x => x.ToString("X2"))) + (b.Length > 16 ? "…" : "") + "]",
        _ => v.ToString() ?? "(?)"
    };

    private static string RenderPacketDiff(PacketDiff d)
    {
        switch (d.Kind)
        {
            case PacketDiffKind.Missing:
                return $"  [{d.Index}] MISSING: expected 0x{(short)d.Expected!.Header:X4} ({d.Expected.Header}, {d.Expected.Length}B) — server sent nothing here";
            case PacketDiffKind.Extra:
                return $"  [{d.Index}] EXTRA:   server sent 0x{(short)d.Actual!.Header:X4} ({d.Actual.Header}, {d.Actual.Length}B) but capture did not";
            case PacketDiffKind.HeaderMismatch:
                return $"  [{d.Index}] HEADER:  expected 0x{(short)d.Expected!.Header:X4} ({d.Expected.Header}) but got 0x{(short)d.Actual!.Header:X4} ({d.Actual.Header})";
            case PacketDiffKind.BodyMismatch:
                var name = d.Expected!.Header.ToString();
                var lines = new StringBuilder();
                lines.AppendLine($"  [{d.Index}] BODY:    0x{(short)d.Expected!.Header:X4} ({name}) bytes differ");
                foreach (var b in d.ByteDiffs.Take(16))
                {
                    if (b.Note != null) lines.AppendLine($"           {b.Note}");
                    else lines.AppendLine($"           @byte {b.Offset:D4}: expected=0x{b.ExpectedByte:X2} actual=0x{b.ActualByte:X2}");
                }
                if (d.ByteDiffs.Count > 16) lines.AppendLine($"           …{d.ByteDiffs.Count - 16} more byte diffs omitted");
                return lines.ToString().TrimEnd();
            case PacketDiffKind.FieldMismatch:
                var fname = d.Expected!.Header.ToString();
                var fl = new StringBuilder();
                fl.AppendLine($"  [{d.Index}] FIELDS:  0x{(short)d.Expected!.Header:X4} ({fname}) field values differ");
                foreach (var f in d.FieldDiffs)
                {
                    fl.AppendLine($"           {f.Name}: expected={FormatValue(f.Expected)} actual={FormatValue(f.Actual)}");
                }
                return fl.ToString().TrimEnd();
            default:
                return $"  [{d.Index}] {d.Kind}";
        }
    }
}

public sealed record ChunkDiff(
    int SourceLine,
    int Port,
    FramingResult Expected,
    FramingResult Actual,
    IReadOnlyList<PacketDiff> PacketDiffs)
{
    public bool IsClean =>
        PacketDiffs.Count == 0
        && Expected.UnknownPackets.Count == 0
        && Actual.UnknownPackets.Count == 0
        && Expected.TrailingBytes == 0
        && Actual.TrailingBytes == 0;
}

public sealed record PacketDiff(
    int Index,
    PacketDiffKind Kind,
    FramedPacket? Expected,
    FramedPacket? Actual,
    IReadOnlyList<ByteDiff> ByteDiffs,
    IReadOnlyList<FieldDiff> FieldDiffs);

public enum PacketDiffKind { BodyMismatch, HeaderMismatch, Missing, Extra, FieldMismatch }

public sealed record ByteDiff(int Offset, byte? ExpectedByte, byte? ActualByte, string? Note);

public sealed record FieldDiff(string Name, object? Expected, object? Actual, bool Tolerant);
