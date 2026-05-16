using System.IO.Compression;
using Map.Server.World;

namespace Map.Server.Tests.World;

public class MapCacheReaderTests
{
    private const string RealMapCachePath = "/Volumes/1TB/Projetos/rathena/db/re/map_cache.dat";

    // --- Synthetic round-trip tests ---

    [Fact]
    public void ParseMainHeader_ReadsFileSizeAndMapCount()
    {
        var header = new byte[8];
        BitConverter.GetBytes((uint)1234).CopyTo(header, 0);
        BitConverter.GetBytes((ushort)42).CopyTo(header, 4);
        // bytes 6-7 are struct padding (skipped, not part of map_count)

        var (mapCount, fileSize) = MapCacheReader.ParseMainHeader(header);

        Assert.Equal((ushort)42, mapCount);
        Assert.Equal((uint)1234, fileSize);
    }

    [Fact]
    public void ParseMainHeader_TooShort_Throws()
    {
        var stub = new byte[3];
        Assert.Throws<InvalidDataException>(() => MapCacheReader.ParseMainHeader(stub));
    }

    [Fact]
    public void ReadMap_SyntheticOneMap_RoundTrips()
    {
        // 3×2 cells: walkable, walkable, blocked, walkable, water, walkable
        var rawCells = new byte[] { 0, 0, 1, 0, 3, 0 };
        var compressed = ZlibCompress(rawCells);
        var bytes = BuildCacheFile(new[] { ("test_map", (short)3, (short)2, compressed) });

        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            var reader = new MapCacheReader();
            var map = reader.ReadMap(tempPath, "test_map");

            Assert.NotNull(map);
            Assert.Equal("test_map", map!.Name);
            Assert.Equal((short)3, map.Xs);
            Assert.Equal((short)2, map.Ys);
            Assert.True(map.IsWalkable(0, 0));
            Assert.False(map.IsWalkable(2, 0));   // blocked
            Assert.True(map.IsWater(1, 1));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void ReadMap_MapNotInCache_ReturnsNull()
    {
        var bytes = BuildCacheFile(new[] {
            ("present", (short)2, (short)2, ZlibCompress(new byte[] { 0, 0, 0, 0 }))
        });
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            var reader = new MapCacheReader();
            Assert.Null(reader.ReadMap(tempPath, "missing"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void ReadAll_MultipleMaps_ParsesAll()
    {
        var bytes = BuildCacheFile(new[] {
            ("alpha", (short)2, (short)2, ZlibCompress(new byte[] { 0, 1, 1, 0 })),
            ("beta",  (short)1, (short)1, ZlibCompress(new byte[] { 0 })),
        });
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            var reader = new MapCacheReader();
            var all = reader.ReadAll(tempPath);

            Assert.Equal(2, all.Count);
            Assert.True(all["alpha"].IsWalkable(0, 0));
            Assert.False(all["alpha"].IsWalkable(1, 0));
            Assert.True(all["beta"].IsWalkable(0, 0));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    // --- Smoke test against the real rAthena renewal cache ---

    [Fact]
    public void ReadMap_RealCacheFirstMap_IsWellFormed()
    {
        if (!File.Exists(RealMapCachePath)) return;

        var reader = new MapCacheReader();
        var all = reader.ReadAll(RealMapCachePath);
        Assert.NotEmpty(all);

        // Pick any map from the real cache; verify a centre-cell read works
        // (the synthetic tests already cover unit-level behavior — this is a
        // smoke check that decompression + indexing line up on real data).
        var any = all.Values.First();
        var midX = (short)(any.Xs / 2);
        var midY = (short)(any.Ys / 2);
        // Reading a valid cell shouldn't throw; the flag value itself isn't
        // asserted (depends on map geometry).
        _ = any.GetCell(midX, midY);
    }

    [Fact]
    public void ReadAll_RealRenewalCache_ParsesEveryMap()
    {
        if (!File.Exists(RealMapCachePath)) return;

        var reader = new MapCacheReader();
        var all = reader.ReadAll(RealMapCachePath);

        // The shipped renewal cache in this rAthena clone contains a small set of
        // dev-friendly maps; the assertion is "every header advertised parsed
        // without throwing and every map has positive dimensions." The exact list
        // depends on the cache that was built by running the mapcache tool.
        Assert.True(all.Count > 0, "Expected at least one map in the cache");
        foreach (var (name, map) in all)
        {
            Assert.False(string.IsNullOrEmpty(name), "Map names should be non-empty");
            Assert.True(map.Xs > 0, $"Map '{name}' has invalid width {map.Xs}");
            Assert.True(map.Ys > 0, $"Map '{name}' has invalid height {map.Ys}");
        }
    }

    // --- helpers ---

    private static byte[] ZlibCompress(byte[] data)
    {
        using var ms = new MemoryStream();
        using (var zs = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            zs.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    private static byte[] BuildCacheFile((string Name, short Xs, short Ys, byte[] Compressed)[] maps)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // main_header: uint32 file_size, uint16 map_count, 2 bytes padding = 8 bytes.
        var totalSize = 8 + maps.Sum(m => 20 + m.Compressed.Length);
        bw.Write((uint)totalSize);
        bw.Write((ushort)maps.Length);
        bw.Write((ushort)0); // struct padding

        foreach (var m in maps)
        {
            var name = new byte[12];
            var nameBytes = System.Text.Encoding.ASCII.GetBytes(m.Name);
            Array.Copy(nameBytes, name, Math.Min(nameBytes.Length, 11));
            bw.Write(name);
            bw.Write(m.Xs);
            bw.Write(m.Ys);
            bw.Write(m.Compressed.Length);
            bw.Write(m.Compressed);
        }

        return ms.ToArray();
    }
}
