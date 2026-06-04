using System.Collections.Concurrent;
using Core.Server.IPC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Items;
using Map.Server.Services;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Auction;

/// <summary>
/// Default <see cref="IAuctionService"/>. Escrows the listed item + zeny map-side, dispatches to the
/// char-side auction RPCs (which own the row + expiry + mail payouts), and caches the last browsed
/// list per character for the bid/buy-now/cancel gates. The char side is the final bid authority.
/// </summary>
public sealed class AuctionService : IAuctionService
{
    // rAthena battle_config defaults.
    private const int FeePerHour = 12000;            // auction_feeperhour
    private const int MaxHours = 48;                 // auction register caps hours at 48
    private const int MaxAuctionPrice = 1_000_000_000; // auction_maximumprice

    private readonly ISessionManagerAccessor? _sessions;
    private readonly ICharServerIpcServiceAuction? _ipc;
    private readonly IItemCatalog? _items;
    private readonly ILogger<AuctionService> _logger;

    // Browsed auctions keyed by auction id (auctions are global) — read-through cache for the
    // bid/buy-now/cancel validation gates. The char side is the final authority.
    private readonly ConcurrentDictionary<long, AuctionData> _cached = new();

    public AuctionService(ILogger<AuctionService> logger,
        ISessionManagerAccessor? sessions = null, ICharServerIpcServiceAuction? ipc = null,
        IItemCatalog? items = null)
    {
        _logger = logger;
        _sessions = sessions;
        _ipc = ipc;
        _items = items;
    }

    public async Task<long> RegisterAsync(PlayerEntity seller, int inventoryIndex, int amount,
        int startPrice, int buyNowPrice, int hours, CancellationToken ct = default)
    {
        // rAthena clif_parse_Auction_register gates.
        if (amount <= 0 || hours < 1 || hours > MaxHours) return 0;
        if (startPrice < 0 || buyNowPrice <= 0 || startPrice >= buyNowPrice) return 0; // "altering prices"
        if (buyNowPrice > MaxAuctionPrice) return 0;

        var session = _sessions?.GetByEntityId(seller.Id);
        var inv = session?.Inventory;
        if (session?.CharacterData == null || inv == null || _ipc == null) return 0;

        var item = FindBySlot(inv, inventoryIndex);
        if (item == null || item.Equip != 0 || item.Amount < (uint)amount) return 0; // gone / equipped / short

        var fee = hours * FeePerHour;
        if ((long)session.CharacterData.Zeny < fee) return 0; // can't pay the listing fee

        // Snapshot for rebound, then escrow the item + fee out of the seller.
        var escrowed = Clone(item, amount);
        DebitSlot(session, inv, inventoryIndex, amount);
        session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny - fee);

        var data = new AuctionData
        {
            SellerCharacterId = seller.CharacterId,
            SellerName = seller.Name,
            ItemId = (int)item.NameId,
            ItemName = _items?.Get(item.NameId)?.NameEnglish ?? string.Empty,
            ItemType = AuctionType(item.NameId),
            Refine = item.Refine,
            Attribute = item.Attribute,
            Price = startPrice,
            BuyNow = buyNowPrice,
            Hours = hours,
            Item = BuildItemData(escrowed, amount),
        };

        var resp = await _ipc.AuctionRegisterAsync(data, ct);
        if (resp is not { Success: true })
        {
            // Rebound the escrow — the row was not created.
            CreditItem(inv, escrowed);
            session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny + fee);
            _logger.LogWarning("auction_register: char rejected; rebounded item + {Fee}z to {Seller}",
                fee, seller.Name);
            return 0;
        }
        // GP-AUCTION: AuctionRegisterHandler emits clif_Auction_message(confirmation) on the id>0 return.
        _logger.LogInformation("auction_register: {Seller} listed item {Item} (start {Start}, buynow {Buy}, {Hours}h, fee {Fee})",
            seller.Name, data.ItemId, startPrice, buyNowPrice, hours, fee);
        return resp.Auction?.AuctionId ?? 0;
    }

    public async Task<bool> BidAsync(PlayerEntity bidder, long auctionId, int bid, CancellationToken ct = default)
    {
        if (bid <= 0) return false;
        var session = _sessions?.GetByEntityId(bidder.Id);
        if (session?.CharacterData == null || _ipc == null) return false;

        // Validate against the cached snapshot (char is the final authority; it refunds a late loser).
        if (_cached.TryGetValue(auctionId, out var cachedAuction))
        {
            if (cachedAuction.SellerCharacterId == bidder.CharacterId) return false; // can't bid your own
            if (cachedAuction.Price > 0 && bid <= cachedAuction.Price) return false; // must beat the high bid
            if (cachedAuction.Price == 0 && bid <= 0) return false;                  // first bid must be positive
        }
        if ((long)session.CharacterData.Zeny < bid) return false;

        session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny - bid); // escrow the bid

        var resp = await _ipc.AuctionBidAsync(bidder.CharacterId, bidder.Name, auctionId, bid, ct);
        if (resp is not { Success: true })
        {
            session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny + bid); // rebound
            return false;
        }
        // GP-AUCTION: AuctionBidHandler emits clif_Auction_message; the char side mails the prior bidder their refund.
        _logger.LogInformation("auction_bid: {Bidder} bid {Bid}z on #{Id}", bidder.Name, bid, auctionId);
        return true;
    }

    public async Task<bool> BuyNowAsync(PlayerEntity buyer, long auctionId, CancellationToken ct = default)
    {
        var session = _sessions?.GetByEntityId(buyer.Id);
        if (session?.CharacterData == null || _ipc == null) return false;
        if (!_cached.TryGetValue(auctionId, out var auction))
            return false; // need the buy-now price from a browsed entry
        if (auction.SellerCharacterId == buyer.CharacterId) return false;
        if ((long)session.CharacterData.Zeny < auction.BuyNow) return false;

        session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny - auction.BuyNow); // escrow

        var resp = await _ipc.AuctionCloseAsync(buyer.CharacterId, auctionId, ct);
        if (resp is not { Success: true })
        {
            session.CharacterData.Zeny = (uint)((long)session.CharacterData.Zeny + auction.BuyNow);
            return false;
        }
        // GP-AUCTION: AuctionCloseHandler emits clif_Auction_close; char-side item/zeny delivery is turn 2.
        _logger.LogInformation("auction_buynow: {Buyer} bought #{Id} for {Price}z", buyer.Name, auctionId, auction.BuyNow);
        return true;
    }

    public async Task<bool> CancelAsync(PlayerEntity seller, long auctionId, CancellationToken ct = default)
    {
        if (_ipc == null) return false;
        // rAthena gate: cannot cancel once a bidder exists.
        if (_cached.TryGetValue(auctionId, out var auction) && auction.BuyerCharacterId != 0)
            return false;

        var resp = await _ipc.AuctionCancelAsync(seller.CharacterId, auctionId, ct);
        if (resp is not { Success: true }) return false;
        // GP-AUCTION: AuctionCancelHandler emits clif_Auction_message; char-side item return is turn 2.
        _logger.LogInformation("auction_cancel: {Seller} cancelled #{Id}", seller.Name, auctionId);
        return true;
    }

    public async Task<AuctionRequestListResponse?> RequestListAsync(PlayerEntity searcher,
        int type, int price, string search, int page, CancellationToken ct = default)
    {
        if (_ipc == null) return null;
        var resp = await _ipc.AuctionRequestListAsync(searcher.CharacterId, type, price, search ?? string.Empty, page, ct);
        if (resp is { Success: true })
        {
            // Cache each browsed auction so the bid/buy-now/cancel gates can read the current price/bidder.
            foreach (var a in resp.Auctions) _cached[a.AuctionId] = a;
        }
        // GP-AUCTION: AuctionSearch/ReqMyInfo handlers render clif_Auction_results from this response.
        return resp;
    }

    /// <summary>FEATURE-06 test seam — seed the browsed-auction cache (keyed by auction id).</summary>
    internal void SeedCacheForTest(AuctionData auction) => _cached[auction.AuctionId] = auction;

    // --- helpers ---

    /// <summary>Map the item's catalog type to the rAthena auction search bucket (0 armor / 1 weapon /
    /// 2 card / 3 etc.). ➡️ Precise bucketing is FEATURE-26.</summary>
    private int AuctionType(uint nameId)
    {
        var t = _items?.Get(nameId)?.Type;
        return t switch
        {
            "Weapon" => 1,
            "Armor" => 0,
            "Card" => 2,
            _ => 3,
        };
    }

    private static InventoryItem? FindBySlot(List<InventoryItem> inv, int serverIndex)
    {
        foreach (var i in inv) if (i.ServerIndex == serverIndex) return i;
        return null;
    }

    /// <summary>Full item fidelity carried on the register RPC (cards/options/uniqueid/bound/grade) so
    /// the auctioned item round-trips with its enchantments through to mail delivery (GP-AUCTION turn 2).</summary>
    private static MailAttachmentItem BuildItemData(InventoryItem i, int amount)
    {
        var a = new MailAttachmentItem
        {
            Index = (uint)i.ServerIndex,
            NameId = i.NameId,
            Amount = (uint)amount,
            Refine = i.Refine,
            Attribute = i.Attribute,
            Identify = i.Identified ? 1 : 0,
            Card0 = i.Card0, Card1 = i.Card1, Card2 = i.Card2, Card3 = i.Card3,
            UniqueId = i.UniqueId,
            Bound = i.Bound,
            EnchantGrade = i.EnchantGrade,
        };
        var opt = i.Options;
        if (opt.Length > 0) { a.OptionId0 = opt[0].Id; a.OptionVal0 = opt[0].Value; a.OptionParm0 = opt[0].Param; }
        if (opt.Length > 1) { a.OptionId1 = opt[1].Id; a.OptionVal1 = opt[1].Value; a.OptionParm1 = opt[1].Param; }
        if (opt.Length > 2) { a.OptionId2 = opt[2].Id; a.OptionVal2 = opt[2].Value; a.OptionParm2 = opt[2].Param; }
        if (opt.Length > 3) { a.OptionId3 = opt[3].Id; a.OptionVal3 = opt[3].Value; a.OptionParm3 = opt[3].Param; }
        if (opt.Length > 4) { a.OptionId4 = opt[4].Id; a.OptionVal4 = opt[4].Value; a.OptionParm4 = opt[4].Param; }
        return a;
    }

    private static InventoryItem Clone(InventoryItem i, int amount) => new()
    {
        ServerIndex = i.ServerIndex, NameId = i.NameId, Amount = (uint)amount,
        Identified = i.Identified, Refine = i.Refine, Attribute = i.Attribute,
        Card0 = i.Card0, Card1 = i.Card1, Card2 = i.Card2, Card3 = i.Card3,
        Options = (ItemOption[])i.Options.Clone(), ExpireTime = i.ExpireTime,
        Bound = i.Bound, UniqueId = i.UniqueId, EnchantGrade = i.EnchantGrade,
    };

    private static void DebitSlot(Map.Server.MapSessionData session, List<InventoryItem> inv, int serverIndex, int amount)
    {
        var item = FindBySlot(inv, serverIndex);
        if (item == null) return;
        item.Amount -= (uint)amount;
        if (item.Amount == 0)
        {
            if (item.Id > 0) session.RemovedInventoryIds.Add(item.Id);
            inv.Remove(item);
        }
    }

    private static void CreditItem(List<InventoryItem> inv, InventoryItem escrowed)
    {
        foreach (var i in inv)
        {
            if (i.NameId != escrowed.NameId || i.Refine != escrowed.Refine) continue;
            if (i.Card0 != escrowed.Card0 || i.Card1 != escrowed.Card1 || i.Card2 != escrowed.Card2 || i.Card3 != escrowed.Card3) continue;
            i.Amount += escrowed.Amount;
            return;
        }
        var nextSlot = 0;
        foreach (var i in inv) if (i.ServerIndex >= nextSlot) nextSlot = i.ServerIndex + 1;
        escrowed.ServerIndex = nextSlot;
        escrowed.Id = 0; // new row
        inv.Add(escrowed);
    }
}
