using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T7.4 — verifies IntifService dispatches the 4 storage entry
/// points (account + guild × load/save) through
/// ICharServerIpcServiceStorage when wired, and short-circuits to 0
/// otherwise. 6 tests covering dispatch + short-circuit.
/// </summary>
public class IntifStorageWiringTests
{
    [Fact]
    public void RequestAccountStorage_WithIpc_Dispatches()
    {
        var fake = new RecordingStorageIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, storageIpc: fake);
        Assert.Equal(1, intif.RequestAccountStorage(accountId: 11));
        Assert.Single(fake.AccountLoadCalls);
        Assert.Equal(11, fake.AccountLoadCalls[0].AccountId);
    }

    [Fact]
    public void RequestAccountStorage_WithoutIpc_ReturnsZero()
    {
        var intif = new IntifService(NullLogger<IntifService>.Instance);
        Assert.Equal(0, intif.RequestAccountStorage(accountId: 11));
    }

    [Fact]
    public void SaveAccountStorage_WithIpc_Dispatches()
    {
        var fake = new RecordingStorageIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, storageIpc: fake);
        var data = new byte[] { 1, 2, 3 };
        Assert.Equal(1, intif.SaveAccountStorage(accountId: 11, data: data));
        Assert.Single(fake.AccountSaveCalls);
        Assert.Equal(11, fake.AccountSaveCalls[0].AccountId);
        Assert.Equal(data, fake.AccountSaveCalls[0].Data);
    }

    [Fact]
    public void RequestGuildStorage_WithIpc_Dispatches()
    {
        var fake = new RecordingStorageIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, storageIpc: fake);
        Assert.Equal(1, intif.RequestGuildStorage(charId: 5, guildId: 42));
        Assert.Single(fake.GuildLoadCalls);
        Assert.Equal(42, fake.GuildLoadCalls[0]);
    }

    [Fact]
    public void SaveGuildStorage_WithIpc_Dispatches()
    {
        var fake = new RecordingStorageIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, storageIpc: fake);
        var data = new byte[] { 0xff, 0xfe };
        Assert.Equal(1, intif.SaveGuildStorage(charId: 5, guildId: 42, data: data));
        Assert.Single(fake.GuildSaveCalls);
        Assert.Equal(42, fake.GuildSaveCalls[0].GuildId);
        Assert.Equal(data, fake.GuildSaveCalls[0].Data);
    }

    [Fact]
    public void SaveGuildStorage_WithoutIpc_ReturnsZero()
    {
        var intif = new IntifService(NullLogger<IntifService>.Instance);
        Assert.Equal(0, intif.SaveGuildStorage(charId: 5, guildId: 42, data: new byte[] { 1 }));
    }

    private sealed class RecordingStorageIpc : ICharServerIpcServiceStorage
    {
        public sealed record AccountLoadCall(int AccountId, long CharacterId);
        public sealed record AccountSaveCall(int AccountId, long CharacterId, byte[] Data);
        public sealed record GuildSaveCall(int GuildId, byte[] Data);

        public List<AccountLoadCall> AccountLoadCalls { get; } = new();
        public List<AccountSaveCall> AccountSaveCalls { get; } = new();
        public List<int> GuildLoadCalls { get; } = new();
        public List<GuildSaveCall> GuildSaveCalls { get; } = new();

        public Task<Core.Server.IPC.GuildStorageLoadResponse?> GuildStorageLoadAsync(
            int guildId, CancellationToken cancellationToken = default)
        {
            GuildLoadCalls.Add(guildId);
            return Task.FromResult<Core.Server.IPC.GuildStorageLoadResponse?>(null);
        }

        public Task<Core.Server.IPC.GuildStorageSaveResponse?> GuildStorageSaveAsync(
            int guildId, byte[] data, CancellationToken cancellationToken = default)
        {
            GuildSaveCalls.Add(new GuildSaveCall(guildId, data));
            return Task.FromResult<Core.Server.IPC.GuildStorageSaveResponse?>(null);
        }

        public Task<Core.Server.IPC.StorageItemboundRetrieveResponse?> StorageItemboundRetrieveAsync(
            int accountId, long characterId, CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Server.IPC.StorageItemboundRetrieveResponse?>(null);

        public Task<Core.Server.IPC.AccountStorageLoadResponse?> AccountStorageLoadAsync(
            int accountId, long characterId, CancellationToken cancellationToken = default)
        {
            AccountLoadCalls.Add(new AccountLoadCall(accountId, characterId));
            return Task.FromResult<Core.Server.IPC.AccountStorageLoadResponse?>(null);
        }

        public Task<Core.Server.IPC.AccountStorageSaveResponse?> AccountStorageSaveAsync(
            int accountId, long characterId, byte[] data,
            CancellationToken cancellationToken = default)
        {
            AccountSaveCalls.Add(new AccountSaveCall(accountId, characterId, data));
            return Task.FromResult<Core.Server.IPC.AccountStorageSaveResponse?>(null);
        }
    }
}
