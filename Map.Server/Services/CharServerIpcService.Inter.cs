using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<InterBroadcastResponse?> InterBroadcastAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string message,
        int color,
        int type,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterBroadcastAsync(new InterBroadcastRequest
        {
            SourceAccountId = sourceAccountId,
            SourceCharacterId = sourceCharacterId,
            Message = message ?? string.Empty,
            Color = color,
            Type = type
        }, cancellationToken: cancellationToken);
    }

    public async Task<InterBroadcastItemResponse?> InterBroadcastItemAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string message,
        int itemId,
        long amount,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterBroadcastItemAsync(new InterBroadcastItemRequest
        {
            SourceAccountId = sourceAccountId,
            SourceCharacterId = sourceCharacterId,
            Message = message ?? string.Empty,
            ItemId = itemId,
            Amount = amount
        }, cancellationToken: cancellationToken);
    }

    public async Task<InterWhisperResponse?> InterWhisperAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string sourceName,
        string targetName,
        string message,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterWhisperAsync(new InterWhisperRequest
        {
            SourceAccountId = sourceAccountId,
            SourceCharacterId = sourceCharacterId,
            SourceName = sourceName ?? string.Empty,
            TargetName = targetName ?? string.Empty,
            Message = message ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<InterWhisperReplyResponse?> InterWhisperReplyAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string sourceName,
        string targetName,
        bool accepted,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterWhisperReplyAsync(new InterWhisperReplyRequest
        {
            SourceAccountId = sourceAccountId,
            SourceCharacterId = sourceCharacterId,
            SourceName = sourceName ?? string.Empty,
            TargetName = targetName ?? string.Empty,
            Accepted = accepted
        }, cancellationToken: cancellationToken);
    }

    public async Task<InterWhisperToGmResponse?> InterWhisperToGmAsync(
        int sourceAccountId,
        long sourceCharacterId,
        string sourceName,
        int minGmLevel,
        string message,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterWhisperToGmAsync(new InterWhisperToGmRequest
        {
            SourceAccountId = sourceAccountId,
            SourceCharacterId = sourceCharacterId,
            SourceName = sourceName ?? string.Empty,
            MinGmLevel = minGmLevel,
            Message = message ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<InterRegistryUpdateResponse?> InterRegistryUpdateAsync(
        int accountId,
        long characterId,
        IEnumerable<InterRegistryEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        var request = new InterRegistryUpdateRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        };
        request.Entries.AddRange(entries);
        return await client.InterRegistryUpdateAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<InterRegistryFetchResponse?> InterRegistryFetchAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterRegistryFetchAsync(new InterRegistryFetchRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<InterNameChangeResponse?> InterNameChangeAsync(
        long characterId,
        string newName,
        int renameType,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterNameChangeAsync(new InterNameChangeRequest
        {
            CharacterId = characterId,
            NewName = newName ?? string.Empty,
            RenameType = renameType
        }, cancellationToken: cancellationToken);
    }

    public async Task<InterAccountInfoResponse?> InterAccountInfoAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.InterAccountInfoAsync(new InterAccountInfoRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }
}
