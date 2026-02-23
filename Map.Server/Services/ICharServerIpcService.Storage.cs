using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceStorage
{
    Task<GuildStorageLoadResponse?> GuildStorageLoadAsync(
        int guildId,
        CancellationToken cancellationToken = default);

    Task<GuildStorageSaveResponse?> GuildStorageSaveAsync(
        int guildId,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<StorageItemboundRetrieveResponse?> StorageItemboundRetrieveAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<AccountStorageLoadResponse?> AccountStorageLoadAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<AccountStorageSaveResponse?> AccountStorageSaveAsync(
        int accountId,
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default);
}
