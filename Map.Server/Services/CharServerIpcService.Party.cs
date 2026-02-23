using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<PartyCreateResponse?> PartyCreateAsync(
        string name,
        int item,
        int item2,
        int leaderAccountId,
        long leaderCharacterId,
        string leaderName,
        int leaderClassId,
        string leaderMapName,
        uint leaderLevel,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyCreateAsync(new PartyCreateRequest
        {
            Name = name ?? string.Empty,
            Item = item,
            Item2 = item2,
            LeaderAccountId = leaderAccountId,
            LeaderCharacterId = leaderCharacterId,
            LeaderName = leaderName ?? string.Empty,
            LeaderClassId = leaderClassId,
            LeaderMapName = leaderMapName ?? string.Empty,
            LeaderLevel = leaderLevel
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyInfoResponse?> PartyInfoAsync(
        int partyId,
        long requestingCharacterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyInfoAsync(new PartyInfoRequest
        {
            PartyId = partyId,
            RequestingCharacterId = requestingCharacterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyAddMemberResponse?> PartyAddMemberAsync(
        int partyId,
        int accountId,
        long characterId,
        string name,
        int classId,
        string mapName,
        uint level,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyAddMemberAsync(new PartyAddMemberRequest
        {
            PartyId = partyId,
            AccountId = accountId,
            CharacterId = characterId,
            Name = name ?? string.Empty,
            ClassId = classId,
            MapName = mapName ?? string.Empty,
            Level = level
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyChangeOptionResponse?> PartyChangeOptionAsync(
        int partyId,
        int accountId,
        int exp,
        int item,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyChangeOptionAsync(new PartyChangeOptionRequest
        {
            PartyId = partyId,
            AccountId = accountId,
            Exp = exp,
            Item = item
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyLeaveResponse?> PartyLeaveAsync(
        int partyId,
        int accountId,
        long characterId,
        string name,
        int withdrawType,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyLeaveAsync(new PartyLeaveRequest
        {
            PartyId = partyId,
            AccountId = accountId,
            CharacterId = characterId,
            Name = name ?? string.Empty,
            WithdrawType = withdrawType
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyChangeMapResponse?> PartyChangeMapAsync(
        int partyId,
        int accountId,
        long characterId,
        bool online,
        uint level,
        string mapName,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyChangeMapAsync(new PartyChangeMapRequest
        {
            PartyId = partyId,
            AccountId = accountId,
            CharacterId = characterId,
            Online = online,
            Level = level,
            MapName = mapName ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyBreakResponse?> PartyBreakAsync(
        int partyId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyBreakAsync(new PartyBreakRequest
        {
            PartyId = partyId
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyMessageResponse?> PartyMessageAsync(
        int partyId,
        int accountId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyMessageAsync(new PartyMessageRequest
        {
            PartyId = partyId,
            AccountId = accountId,
            Message = message ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyLeaderChangeResponse?> PartyLeaderChangeAsync(
        int partyId,
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyLeaderChangeAsync(new PartyLeaderChangeRequest
        {
            PartyId = partyId,
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<PartyShareLevelResponse?> PartyShareLevelAsync(
        uint shareLevel,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.PartyShareLevelAsync(new PartyShareLevelRequest
        {
            ShareLevel = shareLevel
        }, cancellationToken: cancellationToken);
    }
}
