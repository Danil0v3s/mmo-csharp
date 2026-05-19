namespace Map.Server.Storage;

/// <summary>
/// Serializes the storage item list to / from the bytes payload that
/// the char-server IPC carries (<c>AccountStorageLoad/Save</c>).
/// Format is a compact binary record per item; matches no rAthena
/// format directly (rAthena passes the struct via inter-server packet
/// where the C++ ABI handles serialization for us).
/// </summary>
public static class StorageCodec
{
    private const int Version = 1;

    public static byte[] Encode(IReadOnlyList<StorageItem> items)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(Version);
        bw.Write(items.Count);
        foreach (var i in items)
        {
            bw.Write(i.ServerIndex);
            bw.Write(i.NameId);
            bw.Write(i.Amount);
            bw.Write(i.Identified);
            bw.Write(i.Refine);
            bw.Write(i.Attribute);
            bw.Write(i.Card0); bw.Write(i.Card1); bw.Write(i.Card2); bw.Write(i.Card3);
            bw.Write(i.ExpireTime);
            bw.Write(i.Bound);
            bw.Write(i.UniqueId);
            bw.Write(i.EnchantGrade);
        }
        return ms.ToArray();
    }

    public static List<StorageItem> Decode(byte[]? data)
    {
        var list = new List<StorageItem>();
        if (data == null || data.Length == 0) return list;
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        var version = br.ReadInt32();
        if (version != Version) return list; // Future-proof — versioned blob.
        var count = br.ReadInt32();
        for (int n = 0; n < count; n++)
        {
            list.Add(new StorageItem
            {
                ServerIndex = br.ReadInt32(),
                NameId = br.ReadUInt32(),
                Amount = br.ReadUInt32(),
                Identified = br.ReadBoolean(),
                Refine = br.ReadByte(),
                Attribute = br.ReadByte(),
                Card0 = br.ReadUInt32(),
                Card1 = br.ReadUInt32(),
                Card2 = br.ReadUInt32(),
                Card3 = br.ReadUInt32(),
                ExpireTime = br.ReadUInt32(),
                Bound = br.ReadByte(),
                UniqueId = br.ReadUInt64(),
                EnchantGrade = br.ReadByte(),
            });
        }
        return list;
    }
}
