using Core.Database.Context;
using Core.Server.IPC;
using Grpc.Core;
using Login.Server.Repository.Api;
using Login.Server.Repository.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Login.Server.Tests.Services;

/// <summary>
/// End-to-end exercise of the cross-server duplicate-online gRPC contract.
/// LoginGrpcService.IsAccountOnlineAnywhere is the surface char servers call;
/// these tests prove the wrapper correctly translates the repository state into
/// the proto contract char servers depend on.
/// </summary>
public class LoginGrpcServiceCrossServerTests
{
    [Fact]
    public async Task IsAccountOnlineAnywhere_NotOnline_ReturnsIsOnlineFalse()
    {
        var (service, _, _) = BuildService();
        var accountId = NextAccountId();

        var response = await service.IsAccountOnlineAnywhere(
            new AccountOnlineAnywhereRequest { AccountId = accountId, ExcludeCharServerId = 0 },
            new TestCallContext());

        Assert.False(response.IsOnline);
        Assert.Equal(0, response.CharServerId);
    }

    [Fact]
    public async Task IsAccountOnlineAnywhere_OnlineElsewhere_ReturnsCharServerId()
    {
        var (service, repo, _) = BuildService();
        var accountId = NextAccountId();
        repo.SetOnlineUserCharServer(accountId, charServer: 7);

        var response = await service.IsAccountOnlineAnywhere(
            new AccountOnlineAnywhereRequest { AccountId = accountId, ExcludeCharServerId = 99 },
            new TestCallContext());

        Assert.True(response.IsOnline);
        Assert.Equal(7, response.CharServerId);
    }

    [Fact]
    public async Task IsAccountOnlineAnywhere_OnlineOnCallingServer_ReturnsIsOnlineFalse()
    {
        // Server 1 asks "is account online elsewhere, excluding me?" while the player
        // is on server 1. The answer should be false because the only live session
        // *is* on the calling server — there's no cross-server duplicate.
        var (service, repo, _) = BuildService();
        var accountId = NextAccountId();
        repo.SetOnlineUserCharServer(accountId, charServer: 1);

        var response = await service.IsAccountOnlineAnywhere(
            new AccountOnlineAnywhereRequest { AccountId = accountId, ExcludeCharServerId = 1 },
            new TestCallContext());

        Assert.False(response.IsOnline);
    }

    [Fact]
    public async Task IsAccountOnlineAnywhere_PostDisconnect_ReturnsIsOnlineFalse()
    {
        // Char server A had the account; account disconnects; new connect on server B
        // sees the registry as cleared.
        var (service, repo, _) = BuildService();
        var accountId = NextAccountId();
        repo.SetOnlineUserCharServer(accountId, charServer: 3);
        await repo.RemoveOnlineUser(accountId);

        var response = await service.IsAccountOnlineAnywhere(
            new AccountOnlineAnywhereRequest { AccountId = accountId, ExcludeCharServerId = 5 },
            new TestCallContext());

        Assert.False(response.IsOnline);
    }

    // --- helpers ---

    private static int _nextAccountId = 91_000_000;
    private static int NextAccountId() => Interlocked.Increment(ref _nextAccountId);

    private static (LoginGrpcService service, ILoginDataRepository repo, GameDbContext db) BuildService()
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var sp = new ServiceCollection().BuildServiceProvider();
        var repo = new LoginDataRepository(
            sp.GetRequiredService<IServiceScopeFactory>(),
            loggerFactory.CreateLogger<LoginDataRepository>());

        var db = new GameDbContext(
            new DbContextOptionsBuilder<GameDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var service = new LoginGrpcService(
            loggerFactory.CreateLogger<LoginGrpcService>(),
            charServerRegistry: null!,
            connectionService: null!,
            charServerGrpcHandler: null!,
            loginConfig: new LoginServerConfiguration(),
            loginDataRepository: repo,
            loginRepository: null!,
            dbContext: db);

        return (service, repo, db);
    }

    private sealed class TestCallContext : ServerCallContext
    {
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
