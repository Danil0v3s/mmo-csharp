using Login.Server.Model;
using Login.Server.Repository.Impl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Login.Server.Tests.Repository;

/// <summary>
/// P2.7 replay defense: TryConsumeAuthNode must remove the auth node on first
/// successful consume so a replayed LoginId1/LoginId2 pair on a new TCP
/// connection is rejected. Mirrors rAthena's `chrif_auth_failed` / one-time
/// auth-node semantics in chrif.cpp.
/// </summary>
public class AuthNodeReplayTests
{
    [Fact]
    public void TryConsumeAuthNode_FirstCall_Succeeds()
    {
        var repo = CreateRepo();
        var accountId = NextAccountId();
        SeedAuthNode(repo, accountId, loginId1: 111, loginId2: 222, sex: 'M');

        var result = repo.TryConsumeAuthNode(accountId, 111, 222, 'M', out var node);

        Assert.True(result);
        Assert.NotNull(node);
        Assert.Equal(accountId, node!.AccountId);
    }

    [Fact]
    public void TryConsumeAuthNode_SecondCallWithSameCredentials_Fails()
    {
        // This is the replay-defense guarantee. After a successful first consume,
        // the node is removed; a replayed LoginId pair must be rejected.
        var repo = CreateRepo();
        var accountId = NextAccountId();
        SeedAuthNode(repo, accountId, loginId1: 111, loginId2: 222, sex: 'M');

        Assert.True(repo.TryConsumeAuthNode(accountId, 111, 222, 'M', out _));
        Assert.False(repo.TryConsumeAuthNode(accountId, 111, 222, 'M', out var second));
        Assert.Null(second);
    }

    [Fact]
    public void TryConsumeAuthNode_MismatchedCredentials_Fails()
    {
        var repo = CreateRepo();
        var accountId = NextAccountId();
        SeedAuthNode(repo, accountId, loginId1: 111, loginId2: 222, sex: 'M');

        Assert.False(repo.TryConsumeAuthNode(accountId, 111, 333, 'M', out _));
        Assert.False(repo.TryConsumeAuthNode(accountId, 222, 222, 'M', out _));
        Assert.False(repo.TryConsumeAuthNode(accountId, 111, 222, 'F', out _));

        // Original node should still be there for a correct consume.
        Assert.True(repo.TryConsumeAuthNode(accountId, 111, 222, 'M', out _));
    }

    [Fact]
    public void TryConsumeAuthNode_UnknownAccount_Fails()
    {
        var repo = CreateRepo();
        Assert.False(repo.TryConsumeAuthNode(99_999_999, 1, 2, 'M', out _));
    }

    [Fact]
    public void RemoveAuthNode_ClearsTheNode()
    {
        var repo = CreateRepo();
        var accountId = NextAccountId();
        SeedAuthNode(repo, accountId, loginId1: 111, loginId2: 222, sex: 'M');

        repo.RemoveAuthNode(accountId);

        Assert.False(repo.TryConsumeAuthNode(accountId, 111, 222, 'M', out _));
    }

    // --- helpers ---

    private static int _nextAccountId = 92_000_000;
    private static int NextAccountId() => Interlocked.Increment(ref _nextAccountId);

    private static LoginDataRepository CreateRepo()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var sp = new ServiceCollection().BuildServiceProvider();
        var cfg = new LoginServerConfiguration { DisableWebTokenDelay = 0 };
        return new LoginDataRepository(sp.GetRequiredService<IServiceScopeFactory>(),
            cfg,
            loggerFactory.CreateLogger<LoginDataRepository>());
    }

    /// <summary>
    /// Directly inject an AuthNode bypassing AddAuthNode (which requires a session).
    /// Uses reflection because the underlying dictionary is private. This keeps the
    /// test focused on the consume/replay semantics rather than session setup.
    /// </summary>
    private static void SeedAuthNode(LoginDataRepository repo, int accountId, int loginId1, int loginId2, char sex)
    {
        var node = new AuthNode(
            AccountId: accountId,
            LoginId1: loginId1,
            LoginId2: loginId2,
            Ip: 0,
            Sex: sex,
            ClientType: 0);

        var dictField = typeof(LoginDataRepository).GetField(
            "AuthNodeDictionary",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var dict = (System.Collections.IDictionary)dictField.GetValue(null)!;
        var accIdType = typeof(AccountId);
        var accIdInstance = Activator.CreateInstance(accIdType, accountId)!;
        lock (dict)
        {
            dict[accIdInstance] = node;
        }
    }
}
