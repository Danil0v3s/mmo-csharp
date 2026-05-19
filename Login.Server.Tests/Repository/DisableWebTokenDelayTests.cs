using Login.Server;
using Login.Server.Repository.Impl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Login.Server.Tests.Repository;

/// <summary>
/// Pins rAthena <c>account_db_sql_disable_webtoken</c> +
/// <c>account_disable_webtoken_timer</c> parity (account.cpp:935 / 952).
/// The disable on disconnect is deferred for
/// <c>login_config.disable_webtoken_delay</c> ms; when the timer fires
/// it re-checks <c>login_get_online_user</c> and only flips the column
/// to 0 if the user is still offline — a fast reconnect inside the
/// window preserves the token.
///
/// A custom subclass of <see cref="LoginDataRepository"/> overrides
/// <c>UpdateAccountWebTokenEnabled</c> to count calls, since the
/// production path hits a DB scope we don't have in the unit harness.
/// </summary>
public class DisableWebTokenDelayTests
{
    [Fact]
    public async Task RemoveOnlineUser_DelayZero_FlushesImmediately()
    {
        var repo = CreateRepo(delayMs: 0);
        var accountId = NextAccountId();
        // Production callers pair AddOnlineUser with Update; mirror that
        // so the dictionary actually carries the entry.
        var snapshot = await repo.AddOnlineUser(charServer: 1, accountId: accountId);
        repo.Update(snapshot);
        repo.ResetCounts();

        await repo.RemoveOnlineUser(accountId);

        Assert.Equal(1, repo.DisableCalls);
        Assert.Equal(0, repo.EnableCalls);
    }

    [Fact]
    public async Task RemoveOnlineUser_DelayNonZero_DoesNotFlushSynchronously()
    {
        var repo = CreateRepo(delayMs: 30_000);
        var accountId = NextAccountId();
        var snapshot = await repo.AddOnlineUser(1, accountId);
        repo.Update(snapshot);
        repo.ResetCounts();

        await repo.RemoveOnlineUser(accountId);

        // Synchronous return — disable not yet applied. The Scheduler
        // hasn't fired our callback because the deadline is 30 s out.
        Assert.Equal(0, repo.DisableCalls);
        // Dictionary entry IS cleared synchronously (lets a fast
        // reconnect re-add via AddOnlineUser).
        Assert.Null(repo.GetOnlineUser(accountId));
    }

    [Fact]
    public async Task RemoveThenAddBeforeDeadline_SuppressesDisable()
    {
        // The whole point of the delay: a fast disconnect+reconnect
        // inside the window should leave web_auth_token_enabled intact.
        var repo = CreateRepo(delayMs: 80);
        var accountId = NextAccountId();
        var snapshot = await repo.AddOnlineUser(1, accountId);
        repo.Update(snapshot);
        repo.ResetCounts();

        await repo.RemoveOnlineUser(accountId);
        // Reconnect immediately — dictionary repopulates.
        var readded = await repo.AddOnlineUser(1, accountId);
        repo.Update(readded);

        // Let the timer fire and observe the gate suppress the SQL hit.
        await Task.Delay(250);

        Assert.Equal(0, repo.DisableCalls);
        Assert.NotNull(repo.GetOnlineUser(accountId));
    }

    [Fact]
    public async Task DeferredFlush_FiresWhenUserStaysOffline()
    {
        var repo = CreateRepo(delayMs: 80);
        var accountId = NextAccountId();
        var snapshot = await repo.AddOnlineUser(1, accountId);
        repo.Update(snapshot);
        repo.ResetCounts();

        await repo.RemoveOnlineUser(accountId);
        // No reconnect — the gate releases and the flush should run.
        await Task.Delay(250);

        Assert.Equal(1, repo.DisableCalls);
    }

    private static CountingRepository CreateRepo(int delayMs)
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var sp = new ServiceCollection().BuildServiceProvider();
        var cfg = new LoginServerConfiguration { DisableWebTokenDelay = delayMs };
        return new CountingRepository(
            sp.GetRequiredService<IServiceScopeFactory>(),
            cfg,
            loggerFactory.CreateLogger<LoginDataRepository>());
    }

    private static int _nextAccountId = 80_000_000;
    private static int NextAccountId() => Interlocked.Increment(ref _nextAccountId);

    /// <summary>
    /// Records calls to <c>UpdateAccountWebTokenEnabled</c> so the
    /// deferred-flush behavior is observable without a real DB.
    /// </summary>
    private sealed class CountingRepository : LoginDataRepository
    {
        public int EnableCalls;
        public int DisableCalls;

        public CountingRepository(IServiceScopeFactory s, LoginServerConfiguration c, ILogger<LoginDataRepository> l)
            : base(s, c, l) { }

        public void ResetCounts() { EnableCalls = 0; DisableCalls = 0; }

        protected override Task UpdateAccountWebTokenEnabled(int accountId, bool enabled)
        {
            if (enabled) Interlocked.Increment(ref EnableCalls);
            else Interlocked.Increment(ref DisableCalls);
            return Task.CompletedTask;
        }
    }
}
