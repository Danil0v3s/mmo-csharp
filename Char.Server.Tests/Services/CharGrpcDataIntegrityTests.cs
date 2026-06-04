using Char.Server.Services;
using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Services;

public class CharGrpcDataIntegrityTests
{
    // P1.1 — Homunculus skills round-trip

    [Fact]
    public async Task HomunculusSave_ThenLoad_PreservesSkills()
    {
        var (service, db) = CreateService();

        var save = await service.HomunculusSave(new HomunculusSaveRequest
        {
            AccountId = 1,
            Homunculus = new HomunculusData
            {
                HomunculusId = 100,
                CharacterId = 200,
                Name = "Filir",
                Level = 30,
                Hp = 500, MaxHp = 500, Sp = 100, MaxSp = 100,
                Skills =
                {
                    new HomunculusSkillEntry { Id = 8001, Lv = 5 },
                    new HomunculusSkillEntry { Id = 8002, Lv = 3 },
                    new HomunculusSkillEntry { Id = 8003, Lv = 0 }, // zero-lv skips
                    new HomunculusSkillEntry { Id = 0, Lv = 1 }     // zero-id skips
                }
            }
        }, TestServerCallContext.Instance);

        Assert.True(save.Success);

        // Verify DB: only the 2 valid skills persisted
        var persisted = await db.SkillHomunculi.AsNoTracking().Where(s => s.HomunId == 100).ToListAsync();
        Assert.Equal(2, persisted.Count);
        Assert.Contains(persisted, s => s.Id == 8001 && s.Lv == 5);
        Assert.Contains(persisted, s => s.Id == 8002 && s.Lv == 3);

        var load = await service.HomunculusLoad(new HomunculusLoadRequest
        {
            AccountId = 1, HomunculusId = 100
        }, TestServerCallContext.Instance);

        Assert.True(load.Success);
        Assert.NotNull(load.Homunculus);
        Assert.Equal(2, load.Homunculus.Skills.Count);
        Assert.Contains(load.Homunculus.Skills, s => s.Id == 8001 && s.Lv == 5);
        Assert.Contains(load.Homunculus.Skills, s => s.Id == 8002 && s.Lv == 3);
    }

    [Fact]
    public async Task HomunculusSave_OverwritesExistingSkills()
    {
        var (service, db) = CreateService();
        db.Homunculi.Add(new HomunculusEntity { HomunId = 101, CharId = 200, Name = "X", Level = 1 });
        db.SkillHomunculi.AddRange(
            new SkillHomunculusEntity { HomunId = 101, Id = 1000, Lv = 1 },
            new SkillHomunculusEntity { HomunId = 101, Id = 1001, Lv = 2 });
        await db.SaveChangesAsync();

        await service.HomunculusSave(new HomunculusSaveRequest
        {
            AccountId = 1,
            Homunculus = new HomunculusData
            {
                HomunculusId = 101, CharacterId = 200, Name = "X", Level = 1,
                Skills = { new HomunculusSkillEntry { Id = 2000, Lv = 7 } }
            }
        }, TestServerCallContext.Instance);

        var persisted = await db.SkillHomunculi.AsNoTracking().Where(s => s.HomunId == 101).ToListAsync();
        Assert.Single(persisted);
        Assert.Equal(2000, persisted[0].Id);
        Assert.Equal(7, persisted[0].Lv);
    }

    [Fact]
    public async Task HomunculusDelete_AlsoDeletesSkillRows()
    {
        var (service, db) = CreateService();
        db.Homunculi.Add(new HomunculusEntity { HomunId = 102, CharId = 200, Name = "X", Level = 1 });
        db.SkillHomunculi.AddRange(
            new SkillHomunculusEntity { HomunId = 102, Id = 1, Lv = 1 },
            new SkillHomunculusEntity { HomunId = 102, Id = 2, Lv = 2 });
        await db.SaveChangesAsync();

        var resp = await service.HomunculusDelete(new HomunculusDeleteRequest
        {
            HomunculusId = 102
        }, TestServerCallContext.Instance);

        Assert.True(resp.Success);
        Assert.Empty(await db.SkillHomunculi.AsNoTracking().Where(s => s.HomunId == 102).ToListAsync());
    }

    // P1.2 — Auction refund prior bidder

    [Fact]
    public async Task AuctionBid_WhenOutbiddingDifferentBidder_RefundsPriorBidderViaMail()
    {
        var (service, db) = CreateService();
        db.Auctions.Add(new AuctionEntity
        {
            AuctionId = 9000, SellerId = 1, SellerName = "Seller",
            BuyerId = 200, BuyerName = "OldBuyer", Price = 5000, Buynow = 100000,
            NameId = 501, ItemName = "Red Potion", Hours = 12,
            Timestamp = (uint)DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds()
        });
        await db.SaveChangesAsync();

        var resp = await service.AuctionBid(new AuctionBidRequest
        {
            AuctionId = 9000, CharacterId = 300, BidderName = "NewBuyer", Bid = 8000
        }, TestServerCallContext.Instance);

        Assert.True(resp.Success);
        Assert.Equal(1, resp.Result);

        var refundMail = await db.Mails.AsNoTracking()
            .FirstOrDefaultAsync(m => m.DestId == 200);
        Assert.NotNull(refundMail);
        Assert.Equal(5000u, refundMail!.Zeny);
        Assert.Equal("Auction Manager", refundMail.SendName);
        Assert.Contains("higher bid", refundMail.Message);
        Assert.Equal(0, refundMail.SendId);

        // Auction now has new buyer
        var updated = await db.Auctions.AsNoTracking().FirstAsync(a => a.AuctionId == 9000);
        Assert.Equal(300, updated.BuyerId);
        Assert.Equal("NewBuyer", updated.BuyerName);
        Assert.Equal(8000u, updated.Price);
    }

    [Fact]
    public async Task AuctionBid_WhenSameBidderRaises_RefundsToSelf()
    {
        var (service, db) = CreateService();
        db.Auctions.Add(new AuctionEntity
        {
            AuctionId = 9001, SellerId = 1, SellerName = "Seller",
            BuyerId = 300, BuyerName = "Same", Price = 5000, Buynow = 100000,
            NameId = 501, ItemName = "Red Potion", Hours = 12
        });
        await db.SaveChangesAsync();

        await service.AuctionBid(new AuctionBidRequest
        {
            AuctionId = 9001, CharacterId = 300, BidderName = "Same", Bid = 7000
        }, TestServerCallContext.Instance);

        var refund = await db.Mails.AsNoTracking().FirstOrDefaultAsync(m => m.DestId == 300);
        Assert.NotNull(refund);
        Assert.Equal(5000u, refund!.Zeny);
        Assert.Contains("You have placed a higher bid", refund.Message);
    }

    [Fact]
    public async Task AuctionBid_WhenNoPriorBidder_DoesNotCreateRefundMail()
    {
        var (service, db) = CreateService();
        db.Auctions.Add(new AuctionEntity
        {
            AuctionId = 9002, SellerId = 1, SellerName = "Seller",
            BuyerId = 0, BuyerName = "", Price = 1000, Buynow = 100000,
            NameId = 501, ItemName = "Red Potion", Hours = 12
        });
        await db.SaveChangesAsync();

        await service.AuctionBid(new AuctionBidRequest
        {
            AuctionId = 9002, CharacterId = 400, BidderName = "First", Bid = 2000
        }, TestServerCallContext.Instance);

        Assert.Empty(await db.Mails.AsNoTracking().ToListAsync());
    }

    // P1.3 — Mail attachments round-trip

    [Fact]
    public async Task MailSend_WithAttachments_PersistsAndReturnsViaGetAttachment()
    {
        var (service, db) = CreateService();
        db.Characters.Add(new CharEntity { CharId = 700, AccountId = 70, Name = "Recipient", DeleteDate = 0 });
        await db.SaveChangesAsync();

        var send = await service.MailSend(new MailSendRequest
        {
            SenderCharacterId = 600, SenderName = "Sender",
            ReceiverAccountId = 70, ReceiverCharacterId = 700, ReceiverName = "Recipient",
            Title = "Gift", Body = "Enjoy", Zeny = 1500,
            Items =
            {
                new MailAttachmentItem
                {
                    Index = 0, NameId = 501, Amount = 5, Refine = 0, Identify = 1
                },
                new MailAttachmentItem
                {
                    Index = 1, NameId = 1201, Amount = 1, Refine = 4, Identify = 1, EnchantGrade = 2
                }
            }
        }, TestServerCallContext.Instance);

        Assert.True(send.Success);
        Assert.True(send.MailId > 0);

        var attachments = await db.MailAttachments.AsNoTracking()
            .Where(a => a.Id == send.MailId).ToListAsync();
        Assert.Equal(2, attachments.Count);
        Assert.Contains(attachments, a => a.NameId == 501 && a.Amount == 5);
        Assert.Contains(attachments, a => a.NameId == 1201 && a.Refine == 4 && a.EnchantGrade == 2);

        // GetAttachment returns items and clears them
        var get = await service.MailGetAttachment(new MailGetAttachmentRequest
        {
            AccountId = 70, CharacterId = 700, MailId = send.MailId
        }, TestServerCallContext.Instance);

        Assert.True(get.Success);
        Assert.Equal(1500, get.Zeny);
        Assert.Equal(2, get.Items.Count);
        Assert.Contains(get.Items, i => i.NameId == 501 && i.Amount == 5);
        Assert.Contains(get.Items, i => i.NameId == 1201 && i.EnchantGrade == 2);

        // Attachments removed from DB after retrieval
        Assert.Empty(await db.MailAttachments.AsNoTracking().Where(a => a.Id == send.MailId).ToListAsync());
        // Zeny cleared from mail row
        var mailRow = await db.Mails.AsNoTracking().FirstAsync(m => m.Id == send.MailId);
        Assert.Equal(0u, mailRow.Zeny);
    }

    [Fact]
    public async Task MailSend_SkipsZeroAmountOrZeroIdItems()
    {
        var (service, db) = CreateService();
        var send = await service.MailSend(new MailSendRequest
        {
            SenderCharacterId = 600, SenderName = "S",
            ReceiverCharacterId = 700, ReceiverName = "R",
            Title = "t", Body = "b", Zeny = 0,
            Items =
            {
                new MailAttachmentItem { Index = 0, NameId = 501, Amount = 0 }, // skipped
                new MailAttachmentItem { Index = 1, NameId = 0,   Amount = 5 }, // skipped
                new MailAttachmentItem { Index = 2, NameId = 1201, Amount = 1 }
            }
        }, TestServerCallContext.Instance);

        var rows = await db.MailAttachments.AsNoTracking().Where(a => a.Id == send.MailId).ToListAsync();
        Assert.Single(rows);
        Assert.Equal(1201u, rows[0].NameId);
    }

    [Fact]
    public async Task MailInbox_IncludesAttachmentsPerMail()
    {
        var (service, db) = CreateService();
        db.Characters.Add(new CharEntity { CharId = 700, AccountId = 70, Name = "Recipient", DeleteDate = 0 });
        db.Characters.Add(new CharEntity { CharId = 600, AccountId = 60, Name = "Sender", DeleteDate = 0 });
        await db.SaveChangesAsync();

        await service.MailSend(new MailSendRequest
        {
            SenderCharacterId = 600, SenderName = "Sender",
            ReceiverCharacterId = 700, ReceiverName = "Recipient",
            Title = "M1", Body = "b1",
            Items = { new MailAttachmentItem { Index = 0, NameId = 501, Amount = 3 } }
        }, TestServerCallContext.Instance);

        var inbox = await service.MailRequestInbox(new MailRequestInboxRequest
        {
            AccountId = 70, CharacterId = 700
        }, TestServerCallContext.Instance);

        Assert.True(inbox.Success);
        Assert.Single(inbox.Mails);
        Assert.Single(inbox.Mails[0].Items);
        Assert.Equal(501u, inbox.Mails[0].Items[0].NameId);
        Assert.Equal(3u, inbox.Mails[0].Items[0].Amount);
    }

    // --- GP-AUCTION: full-item fidelity + delivery ---

    private static AuctionData CardedListing(int seller = 200, int buyNow = 100_000) => new()
    {
        SellerCharacterId = seller, SellerName = "Seller", ItemId = 1201, ItemName = "Knife",
        ItemType = 1, Refine = 7, Price = 100, BuyNow = buyNow, Hours = 2,
        Item = new MailAttachmentItem
        {
            NameId = 1201, Amount = 1, Refine = 7, Identify = 1,
            Card0 = 4001, Card1 = 4002, Card2 = 4003, Card3 = 4004,
            OptionId0 = 25, OptionVal0 = 10, OptionParm0 = 1, EnchantGrade = 3, UniqueId = 9999,
        },
    };

    [Fact]
    public async Task AuctionRegister_persists_card_and_option_fidelity_and_browse_carries_it_back()
    {
        var (service, _) = CreateService();
        var reg = await service.AuctionRegister(new AuctionRegisterRequest { Auction = CardedListing() }, TestServerCallContext.Instance);
        Assert.True(reg.Success);

        var list = await service.AuctionRequestList(new AuctionRequestListRequest { Type = 6, CharacterId = 200, Page = 1 }, TestServerCallContext.Instance);
        var row = Assert.Single(list.Auctions);
        Assert.Equal(4001u, row.Item.Card0);
        Assert.Equal(4004u, row.Item.Card3);
        Assert.Equal(25, row.Item.OptionId0);
        Assert.Equal(7u, (uint)row.Refine);
        Assert.Equal(3u, row.Item.EnchantGrade);
    }

    [Fact]
    public async Task AuctionSearch_by_category_filters_on_item_type()
    {
        var (service, _) = CreateService();
        await service.AuctionRegister(new AuctionRegisterRequest { Auction = CardedListing() }, TestServerCallContext.Instance); // ItemType 1 = weapon

        var weapons = await service.AuctionRequestList(new AuctionRequestListRequest { Type = 1, CharacterId = 999, Page = 1 }, TestServerCallContext.Instance);
        Assert.Single(weapons.Auctions);
        var armor = await service.AuctionRequestList(new AuctionRequestListRequest { Type = 0, CharacterId = 999, Page = 1 }, TestServerCallContext.Instance);
        Assert.Empty(armor.Auctions);
    }

    [Fact]
    public async Task AuctionClose_delivers_carded_item_to_buyer_and_zeny_to_seller()
    {
        var (service, db) = CreateService();
        var reg = await service.AuctionRegister(new AuctionRegisterRequest { Auction = CardedListing() }, TestServerCallContext.Instance);
        var id = reg.Auction.AuctionId;
        // a bidder is required for the seller to be able to close (sell to high bidder)
        await service.AuctionBid(new AuctionBidRequest { AuctionId = id, CharacterId = 300, BidderName = "Bidder", Bid = 5000 }, TestServerCallContext.Instance);

        var close = await service.AuctionClose(new AuctionCloseRequest { AuctionId = id, CharacterId = 200 }, TestServerCallContext.Instance);
        Assert.True(close.Success);

        var winnerMail = db.Mails.Include(m => m.Attachments).Single(m => m.DestId == 300 && m.Attachments.Any());
        Assert.Equal(1201u, winnerMail.Attachments.First().NameId);
        Assert.Equal(4001u, winnerMail.Attachments.First().Card0); // cards intact
        var sellerMail = db.Mails.Single(m => m.DestId == 200 && m.Zeny > 0);
        Assert.Equal(5000u, sellerMail.Zeny); // the winning bid
        Assert.Empty(db.Auctions); // row removed
    }

    [Fact]
    public async Task AuctionCancel_returns_the_item_to_the_seller()
    {
        var (service, db) = CreateService();
        var reg = await service.AuctionRegister(new AuctionRegisterRequest { Auction = CardedListing() }, TestServerCallContext.Instance);

        var cancel = await service.AuctionCancel(new AuctionCancelRequest { AuctionId = reg.Auction.AuctionId, CharacterId = 200 }, TestServerCallContext.Instance);
        Assert.True(cancel.Success);

        var returnMail = db.Mails.Include(m => m.Attachments).Single(m => m.DestId == 200 && m.Attachments.Any());
        Assert.Equal(1201u, returnMail.Attachments.First().NameId);
        Assert.Equal(4002u, returnMail.Attachments.First().Card1);
        Assert.Empty(db.Auctions);
    }

    [Fact]
    public async Task AuctionBid_buy_now_delivers_item_to_buyer_and_buynow_zeny_to_seller()
    {
        var (service, db) = CreateService();
        var reg = await service.AuctionRegister(new AuctionRegisterRequest { Auction = CardedListing(buyNow: 100_000) }, TestServerCallContext.Instance);

        // bid at/above buy-now → instant buy
        var bid = await service.AuctionBid(new AuctionBidRequest { AuctionId = reg.Auction.AuctionId, CharacterId = 300, BidderName = "Buyer", Bid = 120_000 }, TestServerCallContext.Instance);
        Assert.True(bid.Success);
        Assert.Equal(20_000, bid.RefundZeny); // overage refunded

        var winnerMail = db.Mails.Include(m => m.Attachments).Single(m => m.DestId == 300 && m.Attachments.Any());
        Assert.Equal(4003u, winnerMail.Attachments.First().Card2);
        var sellerMail = db.Mails.Single(m => m.DestId == 200 && m.Zeny > 0);
        Assert.Equal(100_000u, sellerMail.Zeny); // buy-now price, not the 120k bid
        Assert.Empty(db.Auctions);
    }

    // --- Test infrastructure ---

    private static (CharGrpcService service, GameDbContext db) CreateService()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var config = new CharServerConfiguration();

        var packetSystem = new PacketSystem();
        var sessionManager = new SessionManager(
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("tests"),
            config);

        var state = new CharServerState();
        state.SetState(ServerState.Running);

        var loginIpc = new LoginServerIpcService(
            new ServerConnectionService(),
            loggerFactory.CreateLogger<LoginServerIpcService>());

        var charServer = new CharServerImpl(
            config,
            loggerFactory.CreateLogger<CharServerImpl>(),
            new ServiceCollection().BuildServiceProvider(),
            packetSystem,
            sessionManager,
            new ServerConnectionService(),
            state,
            loginIpc,
            new NoOpCharMaintenanceService());

        var dbContext = new GameDbContext(
            new DbContextOptionsBuilder<GameDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var grpc = new CharGrpcService(
            charServer,
            new MapAuthTicketService(),
            new ReturningClientAuthService(loggerFactory.CreateLogger<ReturningClientAuthService>()),
            new MapServerRegistryService(),
            loginIpc,
            new MapServerIpcService(new ServerConnectionService(), loggerFactory.CreateLogger<MapServerIpcService>()),
            new NoOpCharacterRepository(dbContext),
            new NoOpFriendRepository(),
            dbContext,
            config,
            loggerFactory.CreateLogger<CharGrpcService>());

        return (grpc, dbContext);
    }

    private sealed class NoOpFriendRepository : IFriendRepository
    {
        public Task<IReadOnlyList<FriendEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FriendEntity>>(Array.Empty<FriendEntity>());
        public Task<FriendEntity> AddAsync(FriendEntity entity, CancellationToken ct = default)
            => Task.FromResult(entity);
        public Task DeleteAsync(int charId, int friendId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<bool> AreFriendsAsync(int charId, int friendId, CancellationToken ct = default)
            => Task.FromResult(false);
    }

    private sealed class NoOpCharacterRepository(GameDbContext db) : ICharacterRepository
    {
        public async Task<CharEntity?> GetByIdAsync(int charId, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.CharId == charId, ct);

        public async Task<CharEntity?> GetByNameAsync(string name, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name, ct);

        public async Task<IReadOnlyList<CharEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().Where(c => c.AccountId == accountId).ToListAsync(ct);

        public async Task<IReadOnlyList<CharEntity>> GetOnlineCharactersAsync(CancellationToken ct = default)
            => await db.Characters.AsNoTracking().Where(c => c.Online != 0).ToListAsync(ct);

        public async Task<IReadOnlyList<CharEntity>> GetAllAsync(CancellationToken ct = default)
            => await db.Characters.AsNoTracking().ToListAsync(ct);

        public async Task<CharEntity> AddAsync(CharEntity entity, CancellationToken ct = default)
        {
            db.Characters.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity;
        }

        public Task UpdateAsync(CharEntity entity, CancellationToken ct = default)
        {
            db.Characters.Update(entity);
            return db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int charId, CancellationToken ct = default)
        {
            var c = await db.Characters.FirstOrDefaultAsync(x => x.CharId == charId, ct);
            if (c is null) return;
            db.Characters.Remove(c);
            await db.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsAsync(int charId, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().AnyAsync(c => c.CharId == charId, ct);

        public async Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
            => await db.Characters.AsNoTracking().AnyAsync(c => c.Name == name, ct);
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        public static readonly TestServerCallContext Instance = new();

        protected override string MethodCore => "test";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => new();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => new();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
    }
}
