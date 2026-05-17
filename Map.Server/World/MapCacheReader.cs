using System.IO.Compression;
using System.Text;

namespace Map.Server.World;

/// <summary>
/// Binary parser for rAthena's mapcache.dat (renewal-only).
///
/// File layout (little-endian, see <c>src/tool/mapcache.cpp</c> and
/// <c>src/map/map.cpp:3549 map_readfromcache</c>):
///
/// <code>
///   header (6 bytes):
///     uint32 file_size
///     uint16 map_count
///   per map (20-byte header + variable compressed payload):
///     char[12] name        — null-padded; MAP_NAME_LENGTH = 11 + 1
///     int16   xs
///     int16   ys
///     int32   len          — compressed payload length
///     byte[len] cells      — zlib-compressed, raw .gat cell types
/// </code>
///
/// Renewal db_path is <c>/Volumes/1TB/Projetos/rathena/db/re/map_cache.dat</c>.
/// </summary>
public sealed class MapCacheReader
{
    private const int MapNameLength = 12; // rAthena MAP_NAME_LENGTH (11 + null)
    // C struct main_header { uint32 file_size; uint16 map_count; } has sizeof = 8
    // (trailing 2-byte padding for struct alignment). rAthena writes the struct via
    // `fwrite(&header, sizeof(struct main_header), 1, fp)` so the padding bytes are
    // in the file and must be skipped over.
    private const int MainHeaderSize = 8;
    private const int MapInfoSize = MapNameLength + 2 + 2 + 4; // 20, naturally aligned

    /// <summary>
    /// Read the named map from the cache file. Returns null if the map
    /// is not present (caller decides whether to error or skip).
    /// </summary>
    public MapData? ReadMap(string cacheFilePath, string mapName)
    {
        var bytes = File.ReadAllBytes(cacheFilePath);
        return ReadMapFromBytes(bytes, mapName);
    }

    /// <summary>
    /// Read every map from the cache file. Returns a dictionary keyed by
    /// map name. Use this once at startup; subsequent lookups go through
    /// <see cref="IMapWorldRegistry"/>.
    ///
    /// Pass <paramref name="logger"/> to get a per-map line as the cache is
    /// parsed (name, xs, ys, compressed payload size). Useful when a map
    /// the server expects "should be in the cache" but the warmup is
    /// dropping it — the log tells you whether the parser ever saw it.
    /// </summary>
    public IReadOnlyDictionary<string, MapData> ReadAll(string cacheFilePath, ILogger? logger = null)
    {
        var bytes = File.ReadAllBytes(cacheFilePath);
        var (mapCount, fileSize) = ParseMainHeader(bytes);
        logger?.LogInformation(
            "Parsing mapcache: {MapCount} maps declared, file_size={FileSize}, bytes_on_disk={BytesOnDisk} ({Path})",
            mapCount, fileSize, bytes.Length, cacheFilePath);

        var result = new Dictionary<string, MapData>(mapCount, StringComparer.OrdinalIgnoreCase);

        var offset = MainHeaderSize;
        for (var i = 0; i < mapCount; i++)
        {
            var info = ParseMapInfo(bytes, offset);
            offset += MapInfoSize;
            var cells = DecodeCells(bytes.AsSpan(offset, info.Len), info.Xs, info.Ys);
            offset += info.Len;
            logger?.LogInformation(
                "  [{Index}/{Count}] {Name}: xs={Xs} ys={Ys} cells={Cells} compressed={Compressed}B",
                i + 1, mapCount, info.Name, info.Xs, info.Ys, cells.Length, info.Len);
            result[info.Name] = new MapData(info.Name, info.Xs, info.Ys, cells);
        }

        return result;
    }

    internal MapData? ReadMapFromBytes(byte[] bytes, string mapName)
    {
        var (mapCount, _) = ParseMainHeader(bytes);
        var offset = MainHeaderSize;
        for (var i = 0; i < mapCount; i++)
        {
            var info = ParseMapInfo(bytes, offset);
            offset += MapInfoSize;
            if (info.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase))
            {
                var cells = DecodeCells(bytes.AsSpan(offset, info.Len), info.Xs, info.Ys);
                return new MapData(info.Name, info.Xs, info.Ys, cells);
            }
            offset += info.Len;
        }
        return null;
    }

    internal static (ushort MapCount, uint FileSize) ParseMainHeader(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < MainHeaderSize)
        {
            throw new InvalidDataException(
                $"mapcache.dat too short: {bytes.Length} bytes (need at least {MainHeaderSize})");
        }
        var fileSize = BitConverter.ToUInt32(bytes[..4]);
        var mapCount = BitConverter.ToUInt16(bytes.Slice(4, 2));
        return (mapCount, fileSize);
    }

    internal static MapInfo ParseMapInfo(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset + MapInfoSize > bytes.Length)
        {
            throw new InvalidDataException(
                $"mapcache.dat truncated at offset {offset}");
        }
        var nameBytes = bytes.Slice(offset, MapNameLength);
        // Name is null-padded; trim at the first null.
        var nullIdx = nameBytes.IndexOf((byte)0);
        var nameLen = nullIdx < 0 ? MapNameLength : nullIdx;
        var name = Encoding.ASCII.GetString(nameBytes[..nameLen]);
        var xs = BitConverter.ToInt16(bytes.Slice(offset + MapNameLength, 2));
        var ys = BitConverter.ToInt16(bytes.Slice(offset + MapNameLength + 2, 2));
        var len = BitConverter.ToInt32(bytes.Slice(offset + MapNameLength + 4, 4));
        if (len < 0 || offset + MapInfoSize + len > bytes.Length)
        {
            throw new InvalidDataException(
                $"mapcache.dat invalid compressed length {len} for map '{name}' at offset {offset}");
        }
        return new MapInfo(name, xs, ys, len);
    }

    internal static byte[] DecodeCells(ReadOnlySpan<byte> compressed, short xs, short ys)
    {
        var expectedSize = checked(xs * ys);
        var output = new byte[expectedSize];

        // rAthena uses zlib stream (with 2-byte zlib header), not raw deflate.
        // .NET's ZLibStream handles the zlib container directly.
        using var ms = new MemoryStream(compressed.ToArray(), writable: false);
        using var zs = new ZLibStream(ms, CompressionMode.Decompress);

        var written = 0;
        while (written < expectedSize)
        {
            var read = zs.Read(output, written, expectedSize - written);
            if (read == 0) break;
            written += read;
        }

        if (written != expectedSize)
        {
            throw new InvalidDataException(
                $"Cell decompression produced {written} bytes, expected {expectedSize}");
        }
        return output;
    }

    internal readonly record struct MapInfo(string Name, short Xs, short Ys, int Len);
}
