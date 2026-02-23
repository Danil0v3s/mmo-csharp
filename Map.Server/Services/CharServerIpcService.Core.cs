using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<MapServerMapRegistryResponse?> RegisterMapServerMapsAsync(
        int mapServerId,
        IEnumerable<string> mapNames,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        var request = new MapServerMapRegistryRequest { MapServerId = mapServerId };
        request.MapNames.AddRange(mapNames);
        return await client.RegisterMapServerMapsAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<MapServerUserCountResponse?> GetMapServerUserCountAsync(
        int mapServerId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.GetMapServerUserCountAsync(new MapServerUserCountRequest
        {
            MapServerId = mapServerId
        }, cancellationToken: cancellationToken);
    }

    public async Task<MapServerUserCountUpdateResponse?> RegisterMapServerUserCountAsync(
        int mapServerId,
        int userCount,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.RegisterMapServerUserCountAsync(new MapServerUserCountUpdateRequest
        {
            MapServerId = mapServerId,
            UserCount = userCount
        }, cancellationToken: cancellationToken);
    }

    public async Task<MapServerAddressUpdateResponse?> UpdateMapServerAddressAsync(
        int mapServerId,
        uint ip,
        uint port,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.UpdateMapServerAddressAsync(new MapServerAddressUpdateRequest
        {
            MapServerId = mapServerId,
            Ip = ip,
            Port = port
        }, cancellationToken: cancellationToken);
    }

    public async Task<MapServerChangeResponse?> RequestMapServerChangeAsync(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        uint sex,
        uint clientType,
        string mapName,
        uint x,
        uint y,
        int targetMapServerId,
        uint clientIp,
        uint groupId,
        int ttlSeconds,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.RequestMapServerChangeAsync(new MapServerChangeRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            LoginId1 = loginId1,
            LoginId2 = loginId2,
            Sex = sex,
            ClientType = clientType,
            MapName = mapName ?? string.Empty,
            X = x,
            Y = y,
            TargetMapServerId = targetMapServerId,
            ClientIp = clientIp,
            GroupId = groupId,
            TtlSeconds = ttlSeconds
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterMapAuthResponse?> RequestCharacterMapAuthAsync(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        uint sex,
        uint ip,
        bool autotrade,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.RequestCharacterMapAuthAsync(new CharacterMapAuthRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            LoginId1 = loginId1,
            LoginId2 = loginId2,
            Sex = sex,
            Ip = ip,
            Autotrade = autotrade
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterSelectAuthOkResponse?> NotifyCharacterSelectAuthOkAsync(
        int accountId,
        int loginId1,
        int loginId2,
        uint ip,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.NotifyCharacterSelectAuthOkAsync(new CharacterSelectAuthOkRequest
        {
            AccountId = accountId,
            LoginId1 = loginId1,
            LoginId2 = loginId2,
            Ip = ip
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterKeepAliveResponse?> KeepAliveAsync(
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.KeepAliveAsync(new CharacterKeepAliveRequest(), cancellationToken: cancellationToken);
    }

    public async Task<SaveCharacterStateResponse?> SaveCharacterStateAsync(
        int accountId,
        long characterId,
        bool setOfflineAfterSave,
        bool finalSave,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.SaveCharacterStateAsync(new SaveCharacterStateRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            SetOfflineAfterSave = setOfflineAfterSave,
            FinalSave = finalSave
        }, cancellationToken: cancellationToken);
    }

    public async Task<StatusChangeDataResponse?> RequestStatusChangeDataAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestStatusChangeDataAsync(new StatusChangeDataRequest
        {
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<StatusChangeDataSaveResponse?> SaveStatusChangeDataAsync(
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.SaveStatusChangeDataAsync(new StatusChangeDataSaveRequest
        {
            CharacterId = characterId,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }

    public async Task<SkillCooldownLoadResponse?> LoadSkillCooldownAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.LoadSkillCooldownAsync(new SkillCooldownLoadRequest
        {
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<SkillCooldownSaveResponse?> SaveSkillCooldownAsync(
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.SaveSkillCooldownAsync(new SkillCooldownSaveRequest
        {
            CharacterId = characterId,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterOnlineStateResponse?> SetCharacterOfflineAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.SetCharacterOfflineAsync(new CharacterOnlineStateRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterOnlineStateResponse?> SetCharacterOnlineAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.SetCharacterOnlineAsync(new CharacterOnlineStateRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<SetAllCharactersOfflineResponse?> SetAllCharactersOfflineAsync(
        int mapServerId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.SetAllCharactersOfflineAsync(new SetAllCharactersOfflineRequest
        {
            MapServerId = mapServerId
        }, cancellationToken: cancellationToken);
    }

    public async Task<RemoveFriendResponse?> RequestRemoveFriendAsync(
        long characterId,
        long friendCharacterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestRemoveFriendAsync(new RemoveFriendRequest
        {
            CharacterId = characterId,
            FriendCharacterId = friendCharacterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterNameResponse?> RequestCharacterNameAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestCharacterNameAsync(new CharacterNameRequest
        {
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterEmailChangeResponse?> RequestEmailChangeAsync(
        int accountId,
        string currentEmail,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestEmailChangeAsync(new CharacterEmailChangeRequest
        {
            AccountId = accountId,
            CurrentEmail = currentEmail ?? string.Empty,
            NewEmail = newEmail ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<ForwardAccountStatusChangeResponse?> ForwardAccountStatusChangeAsync(
        int accountId,
        uint state,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.ForwardAccountStatusChangeAsync(new ForwardAccountStatusChangeRequest
        {
            AccountId = accountId,
            State = state
        }, cancellationToken: cancellationToken);
    }

    public async Task<DivorceResponse?> RequestDivorceAsync(
        long characterId,
        long partnerCharacterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestDivorceAsync(new DivorceRequest
        {
            CharacterId = characterId,
            PartnerCharacterId = partnerCharacterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterBanResponse?> RequestCharacterBanAsync(
        int accountId,
        long characterId,
        int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestCharacterBanAsync(new CharacterBanRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            DurationSeconds = durationSeconds
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterUnbanResponse?> RequestCharacterUnbanAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestCharacterUnbanAsync(new CharacterUnbanRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<FameUpdateResponse?> UpdateFameAsync(
        int fameType,
        long characterId,
        int value,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.UpdateFameAsync(new FameUpdateRequest
        {
            FameType = fameType,
            CharacterId = characterId,
            Value = value
        }, cancellationToken: cancellationToken);
    }

    public async Task<FameListResponse?> RequestFameListAsync(
        int fameType,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.RequestFameListAsync(new FameListRequest
        {
            FameType = fameType
        }, cancellationToken: cancellationToken);
    }

    public async Task<BonusScriptGetResponse?> GetBonusScriptAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GetBonusScriptAsync(new BonusScriptGetRequest
        {
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<BonusScriptSaveResponse?> SaveBonusScriptAsync(
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.SaveBonusScriptAsync(new BonusScriptSaveRequest
        {
            CharacterId = characterId,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }
    public async Task<MapAuthConsumeResponse?> ValidateCharAuthTicketAsync(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.ConsumeMapAuthTicketAsync(new MapAuthConsumeRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            LoginId1 = loginId1,
            LoginId2 = loginId2
        }, cancellationToken: cancellationToken);
    }

    public async Task<CharacterDataResponse?> GetCharacterDataAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;

        return await client.GetCharacterDataAsync(new CharacterDataRequest
        {
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }
}
