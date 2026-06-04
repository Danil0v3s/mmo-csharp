using Core.Server.IPC;
using Core.Server.Packets.Out.ZC;

namespace Map.Server.Handlers.Auction;

/// <summary>Maps the char-side <see cref="AuctionData"/> rows into the 83-byte browse-results entries,
/// including the item's cards (carried on the <see cref="AuctionData.Item"/> fidelity payload).</summary>
internal static class AuctionResultMapper
{
    public static List<AuctionResultEntry> ToEntries(IEnumerable<AuctionData> auctions)
    {
        var list = new List<AuctionResultEntry>();
        foreach (var a in auctions)
        {
            var it = a.Item; // full fidelity (null on legacy rows)
            list.Add(new AuctionResultEntry
            {
                AuctionId = (uint)a.AuctionId,
                SellerName = a.SellerName ?? string.Empty,
                NameId = (ushort)a.ItemId,
                Type = a.ItemType,
                Amount = 1,
                Identified = it == null || it.Identify != 0,
                Attribute = (byte)Math.Clamp(a.Attribute, 0, byte.MaxValue),
                Refine = (byte)Math.Clamp(a.Refine, 0, byte.MaxValue),
                Card0 = (ushort)(it?.Card0 ?? 0),
                Card1 = (ushort)(it?.Card1 ?? 0),
                Card2 = (ushort)(it?.Card2 ?? 0),
                Card3 = (ushort)(it?.Card3 ?? 0),
                Price = a.Price,
                BuyNow = a.BuyNow,
                BuyerName = a.BuyerName ?? string.Empty,
                DeleteTime = (uint)Math.Max(0, a.EndTimeUnix),
            });
        }
        return list;
    }
}
