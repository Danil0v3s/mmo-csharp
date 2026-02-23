using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceInter
{
    Task<InterBroadcastResponse?> InterBroadcastAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string message,
        int color,
        int type,
        CancellationToken cancellationToken = default);

    Task<InterBroadcastItemResponse?> InterBroadcastItemAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string message,
        int itemId,
        long amount,
        CancellationToken cancellationToken = default);

    Task<InterWhisperResponse?> InterWhisperAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string sourceName,
        string targetName,
        string message,
        CancellationToken cancellationToken = default);

    Task<InterWhisperReplyResponse?> InterWhisperReplyAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string sourceName,
        string targetName,
        bool accepted,
        CancellationToken cancellationToken = default);

    Task<InterWhisperToGmResponse?> InterWhisperToGmAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string sourceName,
        int minGmLevel,
        string message,
        CancellationToken cancellationToken = default);

    Task<InterRegistryUpdateResponse?> InterRegistryUpdateAsync(
        int accountId,
        long characterId,
        IEnumerable<InterRegistryEntry> entries,
        CancellationToken cancellationToken = default);

    Task<InterRegistryFetchResponse?> InterRegistryFetchAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<InterNameChangeResponse?> InterNameChangeAsync(
        long characterId,
        string newName,
        int renameType,
        CancellationToken cancellationToken = default);

    Task<InterAccountInfoResponse?> InterAccountInfoAsync(
        int accountId,
        CancellationToken cancellationToken = default);
}
