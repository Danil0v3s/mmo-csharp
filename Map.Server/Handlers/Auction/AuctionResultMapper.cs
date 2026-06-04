using Core.Server.IPC;
using Core.Server.Packets.Out.ZC;

namespace Map.Server.Handlers.Auction;

/// <summary>Maps the char-side <see cref="AuctionData"/> rows into the 83-byte browse-results entries.
/// Cards default to 0 until char-side full-item fidelity lands (GP-AUCTION turn 2).</summary>
internal static class AuctionResultMapper
{
    public static List<AuctionResultEntry> ToEntries(IEnumerable<AuctionData> auctions)
    {
        var list = new List<AuctionResultEntry>();
        foreach (var a in auctions)
            list.Add(new AuctionResultEntry
            {
                AuctionId = (uint)a.AuctionId,
                SellerName = a.SellerName ?? string.Empty,
                NameId = (ushort)a.ItemId,
                Type = a.ItemType,
                Amount = 1,
                Identified = true,
                Attribute = (byte)Math.Clamp(a.Attribute, 0, byte.MaxValue),
                Refine = (byte)Math.Clamp(a.Refine, 0, byte.MaxValue),
                Price = a.Price,
                BuyNow = a.BuyNow,
                BuyerName = a.BuyerName ?? string.Empty,
                DeleteTime = (uint)Math.Max(0, a.EndTimeUnix),
            });
        return list;
    }
}
