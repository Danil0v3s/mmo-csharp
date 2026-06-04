using Char.Server.Services;
using Core.Database.Context;
using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Char.Server.Tests.Services;

/// <summary>
/// Pins the three rAthena background timers ported by
/// <see cref="CharMaintenanceService"/>:
///
/// <list type="bullet">
///   <item><c>mail_return_timer</c> — int_mail.cpp:317</item>
///   <item><c>mail_delete_timer</c> — int_mail.cpp:321</item>
///   <item><c>char_clan_member_cleanup</c> — char.cpp:2216</item>
/// </list>
///
/// Each pass uses an EF Core InMemory DB scope; seed → tick →
/// re-read, asserting the row transitions.
/// </summary>
public class CharMaintenanceServiceTests
{
    [Fact]
    public async Task MailReturn_OldNormalMailWithAttachments_FlipsToReturnedAndSwapsAddresses()
    {
        await using var ctx = new Harness();
        var oldTime = (uint)DateTimeOffset.UtcNow.AddDays(-20).ToUnixTimeSeconds();
        var mail = await ctx.SeedMailAsync(new MailEntity
        {
            SendId = 100, SendName = "Alice",
            DestId = 200, DestName = "Bob",
            Title = "Hi", Message = "body",
            Time = oldTime,
            Status = 1, // UNREAD
            Type = 0,   // INBOX_NORMAL
            Zeny = 500,
        });

        var n = await ctx.Service.RunMailReturnTickAsync();
        Assert.Equal(1, n);

        var reloaded = await ctx.ReloadMailAsync(mail.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded!.Type); // → INBOX_RETURNED
        Assert.Equal(0, reloaded.Status); // → NEW
        Assert.Equal(100, reloaded.DestId); // sender ↔ dest swap
        Assert.Equal(200, reloaded.SendId);
        Assert.StartsWith("RE:", reloaded.Title);
        Assert.True(reloaded.Time > oldTime);
    }

    // --- GP-AUCTION turn 3: expiry sweep ---

    private static AuctionEntity ExpiredAuction(int seller, int buyer, uint price, uint nameId = 1201, uint card0 = 4001) => new()
    {
        SellerId = seller, SellerName = "Seller", BuyerId = buyer, BuyerName = buyer > 0 ? "Winner" : "",
        NameId = nameId, ItemName = "Knife", Type = 1, Refine = 7, Card0 = card0, EnchantGrade = 2,
        Price = price, Buynow = 100_000, Hours = 1,
        Timestamp = (uint)DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(), // already past
    };

    [Fact]
    public async Task AuctionExpiry_WithBidder_DeliversItemToWinnerAndZenyToSeller()
    {
        await using var ctx = new Harness();
        await ctx.SeedAuctionAsync(ExpiredAuction(seller: 200, buyer: 300, price: 5000));

        var n = await ctx.Service.RunAuctionExpiryTickAsync();
        Assert.Equal(1, n);

        var (mails, auctions) = await ctx.SnapshotAsync();
        Assert.Equal(0, auctions); // row removed
        var winner = mails.Single(m => m.DestId == 300 && m.Attachments.Any());
        Assert.Equal(1201u, winner.Attachments.First().NameId);
        Assert.Equal(4001u, winner.Attachments.First().Card0); // cards intact
        var seller = mails.Single(m => m.DestId == 200 && m.Zeny > 0);
        Assert.Equal(5000u, seller.Zeny); // the winning bid
    }

    [Fact]
    public async Task AuctionExpiry_NoBidder_ReturnsItemToSeller()
    {
        await using var ctx = new Harness();
        await ctx.SeedAuctionAsync(ExpiredAuction(seller: 200, buyer: 0, price: 0, card0: 4002));

        var n = await ctx.Service.RunAuctionExpiryTickAsync();
        Assert.Equal(1, n);

        var (mails, auctions) = await ctx.SnapshotAsync();
        Assert.Equal(0, auctions);
        var returned = mails.Single(m => m.DestId == 200 && m.Attachments.Any());
        Assert.Equal(4002u, returned.Attachments.First().Card0);
        Assert.DoesNotContain(mails, m => m.Zeny > 0); // no zeny payout when unsold
    }

    [Fact]
    public async Task AuctionExpiry_LiveAuction_IsNotTouched()
    {
        await using var ctx = new Harness();
        var live = ExpiredAuction(seller: 200, buyer: 0, price: 0);
        live.Timestamp = (uint)DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(); // future
        await ctx.SeedAuctionAsync(live);

        var n = await ctx.Service.RunAuctionExpiryTickAsync();
        Assert.Equal(0, n);
        var (mails, auctions) = await ctx.SnapshotAsync();
        Assert.Equal(1, auctions);
        Assert.Empty(mails);
    }

    [Fact]
    public async Task MailReturn_EmptyMail_WithReturnEmptyZero_IsSkipped()
    {
        await using var ctx = new Harness(returnEmpty: 0);
        var oldTime = (uint)DateTimeOffset.UtcNow.AddDays(-20).ToUnixTimeSeconds();
        var mail = await ctx.SeedMailAsync(new MailEntity
        {
            SendId = 100, SendName = "Alice",
            DestId = 200, DestName = "Bob",
            Title = "empty", Time = oldTime, Type = 0, Zeny = 0,
        });

        var n = await ctx.Service.RunMailReturnTickAsync();
        Assert.Equal(0, n);

        // Untouched.
        var reloaded = await ctx.ReloadMailAsync(mail.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(0, reloaded!.Type);
        Assert.Equal(100, reloaded.SendId);
    }

    [Fact]
    public async Task MailReturn_ServerSentMail_IsDeletedNotReturned()
    {
        await using var ctx = new Harness();
        var oldTime = (uint)DateTimeOffset.UtcNow.AddDays(-20).ToUnixTimeSeconds();
        var mail = await ctx.SeedMailAsync(new MailEntity
        {
            SendId = 0, // server
            SendName = "Auction Manager",
            DestId = 200, DestName = "Bob",
            Title = "refund", Time = oldTime, Type = 0, Zeny = 1000,
        });

        await ctx.Service.RunMailReturnTickAsync();

        Assert.Null(await ctx.ReloadMailAsync(mail.Id));
    }

    [Fact]
    public async Task MailReturn_RecentMail_IsLeftAlone()
    {
        await using var ctx = new Harness();
        var fresh = (uint)DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        var mail = await ctx.SeedMailAsync(new MailEntity
        {
            SendId = 100, DestId = 200, Title = "fresh",
            Time = fresh, Type = 0, Zeny = 50,
        });

        var n = await ctx.Service.RunMailReturnTickAsync();
        Assert.Equal(0, n);
        Assert.NotNull(await ctx.ReloadMailAsync(mail.Id));
    }

    [Fact]
    public async Task MailDelete_OldReturnedMails_AreRemoved()
    {
        await using var ctx = new Harness();
        var ancient = (uint)DateTimeOffset.UtcNow.AddDays(-200).ToUnixTimeSeconds();
        var m1 = await ctx.SeedMailAsync(new MailEntity { SendId = 1, DestId = 2, Title = "a", Time = ancient, Type = 1 });
        var m2 = await ctx.SeedMailAsync(new MailEntity { SendId = 1, DestId = 2, Title = "b", Time = ancient, Type = 1 });
        // Fresh normal mail untouched.
        var m3 = await ctx.SeedMailAsync(new MailEntity
        {
            SendId = 1, DestId = 2, Title = "c",
            Time = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = 0,
        });

        var removed = await ctx.Service.RunMailDeleteTickAsync();
        Assert.Equal(2, removed);
        Assert.Null(await ctx.ReloadMailAsync(m1.Id));
        Assert.Null(await ctx.ReloadMailAsync(m2.Id));
        Assert.NotNull(await ctx.ReloadMailAsync(m3.Id));
    }

    [Fact]
    public async Task ClanCleanup_InactiveOfflineCharsAreUnclanned()
    {
        await using var ctx = new Harness(clanInactiveDays: 30);
        var stale = await ctx.SeedCharAsync(new CharEntity
        {
            Name = "OldFan", ClanId = 5, Online = 0,
            LastLogin = DateTime.UtcNow.AddDays(-60),
        });
        var freshOffline = await ctx.SeedCharAsync(new CharEntity
        {
            Name = "Newer", ClanId = 5, Online = 0,
            LastLogin = DateTime.UtcNow.AddDays(-5),
        });
        var activeOnline = await ctx.SeedCharAsync(new CharEntity
        {
            Name = "ActiveAce", ClanId = 5, Online = 1,
            LastLogin = DateTime.UtcNow.AddDays(-90),
        });

        var n = await ctx.Service.RunClanCleanupTickAsync();
        Assert.Equal(1, n);
        Assert.Equal(0, (await ctx.ReloadCharAsync(stale.CharId))!.ClanId);
        Assert.Equal(5, (await ctx.ReloadCharAsync(freshOffline.CharId))!.ClanId);
        Assert.Equal(5, (await ctx.ReloadCharAsync(activeOnline.CharId))!.ClanId);
    }

    [Fact]
    public async Task OnlineCleanup_FlipsCharsWhoseLastMapIsOnNoRegisteredMapServer()
    {
        var registry = new MapServerRegistryService();
        registry.RegisterMaps(1, new[] { "prontera", "geffen" });

        await using var ctx = new Harness(mapRegistry: registry);
        var hosted = await ctx.SeedCharAsync(new CharEntity
        {
            Name = "Hosted", Online = 1, LastMap = "prontera",
        });
        var orphan = await ctx.SeedCharAsync(new CharEntity
        {
            Name = "Orphan", Online = 1, LastMap = "morroc",
        });
        var nomap = await ctx.SeedCharAsync(new CharEntity
        {
            Name = "Nomap", Online = 1, LastMap = string.Empty,
        });

        var n = await ctx.Service.RunOnlineCleanupTickAsync();
        Assert.Equal(2, n);
        Assert.Equal(1, (await ctx.ReloadCharAsync(hosted.CharId))!.Online);
        Assert.Equal(0, (await ctx.ReloadCharAsync(orphan.CharId))!.Online);
        Assert.Equal(0, (await ctx.ReloadCharAsync(nomap.CharId))!.Online);
    }

    [Fact]
    public async Task DisabledKnob_NoOps()
    {
        await using var ctx = new Harness(mailReturnDays: 0, mailDeleteDays: 0, clanInactiveDays: 0);
        var oldTime = (uint)DateTimeOffset.UtcNow.AddDays(-100).ToUnixTimeSeconds();
        await ctx.SeedMailAsync(new MailEntity { SendId = 1, DestId = 2, Title = "x", Time = oldTime, Type = 0, Zeny = 1 });
        await ctx.SeedMailAsync(new MailEntity { SendId = 1, DestId = 2, Title = "y", Time = oldTime, Type = 1 });
        await ctx.SeedCharAsync(new CharEntity
        {
            Name = "Stale", ClanId = 5, Online = 0,
            LastLogin = DateTime.UtcNow.AddDays(-1000),
        });

        Assert.Equal(0, await ctx.Service.RunMailReturnTickAsync());
        Assert.Equal(0, await ctx.Service.RunMailDeleteTickAsync());
        Assert.Equal(0, await ctx.Service.RunClanCleanupTickAsync());
    }

    // --- harness ---

    private sealed class Harness : IAsyncDisposable
    {
        private readonly string _dbName;
        public CharServerConfiguration Config { get; }
        public IServiceScopeFactory Scopes { get; }
        public CharMaintenanceService Service { get; }

        public Harness(
            int mailReturnDays = 15,
            int mailDeleteDays = 90,
            int clanInactiveDays = 14,
            int returnEmpty = 1,
            IMapServerRegistryService? mapRegistry = null)
        {
            _dbName = Guid.NewGuid().ToString();
            Config = new CharServerConfiguration
            {
                MailReturnDays = mailReturnDays,
                MailDeleteDays = mailDeleteDays,
                ClanRemoveInactiveDays = clanInactiveDays,
                MailReturnEmpty = returnEmpty,
            };
            var services = new ServiceCollection();
            services.AddDbContext<GameDbContext>(o => o.UseInMemoryDatabase(_dbName));
            var sp = services.BuildServiceProvider();
            Scopes = sp.GetRequiredService<IServiceScopeFactory>();
            MapRegistry = mapRegistry ?? new MapServerRegistryService();
            Service = new CharMaintenanceService(
                Scopes, Config, MapRegistry,
                NullLogger<CharMaintenanceService>.Instance);
        }

        public IMapServerRegistryService MapRegistry { get; }

        public async Task<MailEntity> SeedMailAsync(MailEntity mail)
        {
            using var scope = Scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            db.Mails.Add(mail);
            await db.SaveChangesAsync();
            return mail;
        }

        public async Task<MailEntity?> ReloadMailAsync(long id)
        {
            using var scope = Scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            return await db.Mails.FindAsync(id);
        }

        public async Task<CharEntity> SeedCharAsync(CharEntity ch)
        {
            using var scope = Scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            db.Characters.Add(ch);
            await db.SaveChangesAsync();
            return ch;
        }

        public async Task<CharEntity?> ReloadCharAsync(int charId)
        {
            using var scope = Scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            return await db.Characters.FindAsync(charId);
        }

        public async Task<AuctionEntity> SeedAuctionAsync(AuctionEntity a)
        {
            using var scope = Scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            db.Auctions.Add(a);
            await db.SaveChangesAsync();
            return a;
        }

        public async Task<(List<MailEntity> mails, int auctions)> SnapshotAsync()
        {
            using var scope = Scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var mails = await db.Mails.Include(m => m.Attachments).ToListAsync();
            var auctions = await db.Auctions.CountAsync();
            return (mails, auctions);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
