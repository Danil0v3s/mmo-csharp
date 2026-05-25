namespace Map.Server.Status;

/// <summary>
/// Map-side decoder for the binary payload returned by
/// <c>CharGrpcService.RequestStatusChangeData</c>
/// (Char.Server/CharGrpcService.cs:4531 SerializeStatusChangeRows). Matches
/// the char-side encoder byte-for-byte:
/// <code>
///   uint8  version  (= 1)
///   int32  count
///   { int32 accountId, int32 charId, uint16 type, int64 tick,
///     int32 val1, int32 val2, int32 val3, int32 val4 } × count
/// </code>
/// </summary>
public static class ScDataCodec
{
    /// <summary>Decode rows or return an empty list on malformed payload.</summary>
    public static List<ScDataEntry> Decode(byte[] payload)
    {
        var rows = new List<ScDataEntry>();
        if (payload == null || payload.Length == 0) return rows;
        try
        {
            using var ms = new MemoryStream(payload);
            using var r = new BinaryReader(ms);
            var version = r.ReadByte();
            if (version != 1) return rows;
            var count = r.ReadInt32();
            if (count < 0) return rows;
            for (int i = 0; i < count; i++)
            {
                rows.Add(new ScDataEntry(
                    r.ReadInt32(),  // AccountId
                    r.ReadInt32(),  // CharId
                    r.ReadUInt16(), // Type
                    r.ReadInt64(),  // Tick (remaining ms)
                    r.ReadInt32(),  // Val1
                    r.ReadInt32(),  // Val2
                    r.ReadInt32(),  // Val3
                    r.ReadInt32()   // Val4
                ));
            }
            return rows;
        }
        catch
        {
            // Malformed payload — drop. rAthena does the same on bad chrif rows.
            return new List<ScDataEntry>();
        }
    }
}

/// <summary>One row of the persisted sc_data table. Maps 1:1 to
/// <c>Core.Database.Entities.ScDataEntity</c> but lives in
/// <c>Map.Server.Status</c> so the map side doesn't depend on EF.</summary>
public sealed record ScDataEntry(
    int AccountId,
    int CharId,
    ushort Type,
    long TickMs,
    int Val1,
    int Val2,
    int Val3,
    int Val4);
