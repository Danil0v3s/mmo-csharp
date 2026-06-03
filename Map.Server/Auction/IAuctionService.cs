using Core.Server.IPC;
using Map.Server.Entities;

namespace Map.Server.Auction;

/// <summary>
/// FEATURE-06 — map-side auction entry points (rAthena <c>clif_parse_Auction_*</c>). The map escrows
/// the listed item out of the seller's inventory + the listing fee / bid zeny, then dispatches to the
/// char-side auction RPCs which own the auction table, the expiry timer, and all payouts/refunds
/// (which travel by mail — FEATURE-05). The char side is the final authority on the current high bid.
/// </summary>
public interface IAuctionService
{
    /// <summary>rAthena <c>clif_parse_Auction_register</c>: list an inventory item. Escrows the item +
    /// the listing fee (<c>hours * feePerHour</c>). Returns the allocated auction id, or 0 on any gate
    /// failure (no escrow).</summary>
    Task<long> RegisterAsync(PlayerEntity seller, int inventoryIndex, int amount,
        int startPrice, int buyNowPrice, int hours, CancellationToken ct = default);

    /// <summary>rAthena <c>clif_parse_Auction_bid</c>: bid on an auction. Validates against the cached
    /// high bid + start price, escrows the bidder's zeny. The char side refunds the prior bidder by
    /// mail. Returns false (no debit) on a gate failure.</summary>
    Task<bool> BidAsync(PlayerEntity bidder, long auctionId, int bid, CancellationToken ct = default);

    /// <summary>rAthena buy-now (<c>clif_parse_Auction_buysell</c> → <c>intif_Auction_close</c>):
    /// pay the buy-now price; the char side mails the item to the buyer + zeny to the seller.</summary>
    Task<bool> BuyNowAsync(PlayerEntity buyer, long auctionId, CancellationToken ct = default);

    /// <summary>rAthena <c>clif_parse_Auction_cancelreg</c>: cancel a listing. Rejected if a bidder
    /// already exists (rAthena gate); otherwise the char side returns the item by mail.</summary>
    Task<bool> CancelAsync(PlayerEntity seller, long auctionId, CancellationToken ct = default);

    /// <summary>rAthena <c>clif_parse_Auction_search</c>: filtered/paged browse. Caches the response
    /// for the bid/buy-now/cancel validation gates.</summary>
    Task<AuctionRequestListResponse?> RequestListAsync(PlayerEntity searcher,
        int type, int price, string search, int page, CancellationToken ct = default);
}
