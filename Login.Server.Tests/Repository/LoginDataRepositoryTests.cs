using Login.Server.Repository.Impl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Login.Server.Tests.Repository;

/// <summary>
/// LoginDataRepository owns the global account-online registry used by the
/// cross-server duplicate-online check (P3). The repository uses static state,
/// so tests pick unique account ids to avoid interference.
/// </summary>
public class LoginDataRepositoryTests
{
    [Fact]
    public void GetOnlineUser_ReturnsNull_WhenNotOnline()
    {
        var repo = CreateRepo();
        Assert.Null(repo.GetOnlineUser(10001));
    }

    [Fact]
    public void SetOnlineUserCharServer_ThenGetOnlineUser_ReturnsSameCharServer()
    {
        var repo = CreateRepo();
        var accountId = NextAccountId();

        repo.SetOnlineUserCharServer(accountId, charServer: 42);
        var entry = repo.GetOnlineUser(accountId);

        Assert.NotNull(entry);
        Assert.Equal(42, entry!.CharServer);
    }

    [Fact]
    public void SetOnlineUserCharServer_TwiceWithDifferentServers_LatestWins()
    {
        var repo = CreateRepo();
        var accountId = NextAccountId();

        repo.SetOnlineUserCharServer(accountId, charServer: 1);
        repo.SetOnlineUserCharServer(accountId, charServer: 2);

        Assert.Equal(2, repo.GetOnlineUser(accountId)!.CharServer);
    }

    [Fact]
    public async Task RemoveOnlineUser_ClearsState()
    {
        var repo = CreateRepo();
        var accountId = NextAccountId();

        repo.SetOnlineUserCharServer(accountId, charServer: 5);
        await repo.RemoveOnlineUser(accountId);

        Assert.Null(repo.GetOnlineUser(accountId));
    }

    [Fact]
    public async Task RemoveOnlineUsersByCharServer_OnlyClearsThatServer()
    {
        var repo = CreateRepo();
        var acc1 = NextAccountId();
        var acc2 = NextAccountId();
        var acc3 = NextAccountId();

        repo.SetOnlineUserCharServer(acc1, charServer: 100);
        repo.SetOnlineUserCharServer(acc2, charServer: 100);
        repo.SetOnlineUserCharServer(acc3, charServer: 101);

        var removed = await repo.RemoveOnlineUsersByCharServer(100);

        Assert.Equal(2, removed);
        Assert.Null(repo.GetOnlineUser(acc1));
        Assert.Null(repo.GetOnlineUser(acc2));
        Assert.NotNull(repo.GetOnlineUser(acc3));
        Assert.Equal(101, repo.GetOnlineUser(acc3)!.CharServer);
    }

    // P7 cross-server duplicate-online scenario:
    // Player connects to char server 1 → SetOnlineUserCharServer(acct, 1).
    // Player attempts new connection on char server 2 → IsAccountOnlineAnywhere
    // (modeled by GetOnlineUser + char_server != exclude check) sees server 1
    // and returns IsOnline=true. The char-side ResolveKickTargetServerId then
    // initiates the kick. This test exercises the state-machinery half.

    [Fact]
    public void CrossServerDuplicateOnline_LookupExcludesCallingServer()
    {
        var repo = CreateRepo();
        var accountId = NextAccountId();

        // Char server 1 reports the account online.
        repo.SetOnlineUserCharServer(accountId, charServer: 1);

        // Char server 2 asks: "is this account online elsewhere (i.e. not on me)?"
        var entry = repo.GetOnlineUser(accountId);
        Assert.NotNull(entry);
        var callerExcluded = entry!.CharServer != 2;
        Assert.True(callerExcluded, "Server 2 should see account 1 as live elsewhere");

        // Same query from char server 1 (the holder) → not "elsewhere".
        var sameServerExcluded = entry.CharServer != 1;
        Assert.False(sameServerExcluded, "Server 1 should not see its own session as 'elsewhere'");
    }

    [Fact]
    public async Task MapServerCrash_RemovesAllOnlineUsersForThatServer()
    {
        // rAthena chrif.cpp on map server disconnect: chrif_disconnect calls
        // chlogif_send_setalloffline which clears the online db for that map.
        // C# equivalent: RemoveOnlineUsersByCharServer batches the cleanup.
        var repo = CreateRepo();
        var serverId = 200;
        var accounts = new[] { NextAccountId(), NextAccountId(), NextAccountId() };
        foreach (var acc in accounts)
        {
            repo.SetOnlineUserCharServer(acc, charServer: serverId);
        }

        var removed = await repo.RemoveOnlineUsersByCharServer(serverId);

        Assert.Equal(accounts.Length, removed);
        foreach (var acc in accounts)
        {
            Assert.Null(repo.GetOnlineUser(acc));
        }
    }

    // --- helpers ---

    private static int _nextAccountId = 90_000_000;
    private static int NextAccountId() => Interlocked.Increment(ref _nextAccountId);

    private static LoginDataRepository CreateRepo()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var sp = new ServiceCollection().BuildServiceProvider();
        return new LoginDataRepository(sp.GetRequiredService<IServiceScopeFactory>(),
            loggerFactory.CreateLogger<LoginDataRepository>());
    }
}
