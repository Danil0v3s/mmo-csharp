using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<GuildStorageLoadResponse?> GuildStorageLoadAsync(
        int guildId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildStorageLoadAsync(new GuildStorageLoadRequest
        {
            GuildId = guildId
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildStorageSaveResponse?> GuildStorageSaveAsync(
        int guildId,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildStorageSaveAsync(new GuildStorageSaveRequest
        {
            GuildId = guildId,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }

    public async Task<StorageItemboundRetrieveResponse?> StorageItemboundRetrieveAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.StorageItemboundRetrieveAsync(new StorageItemboundRetrieveRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountStorageLoadResponse?> AccountStorageLoadAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AccountStorageLoadAsync(new AccountStorageLoadRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountStorageSaveResponse?> AccountStorageSaveAsync(
        int accountId,
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AccountStorageSaveAsync(new AccountStorageSaveRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }
}
