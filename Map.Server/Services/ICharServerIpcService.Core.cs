using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceCore
{
    Task<MapServerMapRegistryResponse?> RegisterMapServerMapsAsync(
        int mapServerId,
        IEnumerable<string> mapNames,
        CancellationToken cancellationToken = default);

    Task<MapServerUserCountResponse?> GetMapServerUserCountAsync(
        int mapServerId,
        CancellationToken cancellationToken = default);

    Task<MapServerUserCountUpdateResponse?> RegisterMapServerUserCountAsync(
        int mapServerId,
        int userCount,
        CancellationToken cancellationToken = default);

    Task<MapServerAddressUpdateResponse?> UpdateMapServerAddressAsync(
        int mapServerId,
        uint ip,
        uint port,
        CancellationToken cancellationToken = default);

    Task<MapServerChangeResponse?> RequestMapServerChangeAsync(
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
        CancellationToken cancellationToken = default);

    Task<CharacterMapAuthResponse?> RequestCharacterMapAuthAsync(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        uint sex,
        uint ip,
        bool autotrade,
        CancellationToken cancellationToken = default);

    Task<CharacterSelectAuthOkResponse?> NotifyCharacterSelectAuthOkAsync(
        int accountId,
        int loginId1,
        int loginId2,
        uint ip,
        CancellationToken cancellationToken = default);

    Task<CharacterKeepAliveResponse?> KeepAliveAsync(
        CancellationToken cancellationToken = default);

    Task<SaveCharacterStateResponse?> SaveCharacterStateAsync(
        int accountId,
        long characterId,
        bool setOfflineAfterSave,
        bool finalSave,
        CancellationToken cancellationToken = default);

    Task<StatusChangeDataResponse?> RequestStatusChangeDataAsync(
        long characterId,
        CancellationToken cancellationToken = default);

    Task<StatusChangeDataSaveResponse?> SaveStatusChangeDataAsync(
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<SkillCooldownLoadResponse?> LoadSkillCooldownAsync(
        long characterId,
        CancellationToken cancellationToken = default);

    Task<SkillCooldownSaveResponse?> SaveSkillCooldownAsync(
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<CharacterOnlineStateResponse?> SetCharacterOfflineAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<CharacterOnlineStateResponse?> SetCharacterOnlineAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<SetAllCharactersOfflineResponse?> SetAllCharactersOfflineAsync(
        int mapServerId,
        CancellationToken cancellationToken = default);

    Task<RemoveFriendResponse?> RequestRemoveFriendAsync(
        long characterId,
        long friendCharacterId,
        CancellationToken cancellationToken = default);

    Task<CharacterNameResponse?> RequestCharacterNameAsync(
        long characterId,
        CancellationToken cancellationToken = default);

    Task<CharacterEmailChangeResponse?> RequestEmailChangeAsync(
        int accountId,
        string currentEmail,
        string newEmail,
        CancellationToken cancellationToken = default);

    Task<ForwardAccountStatusChangeResponse?> ForwardAccountStatusChangeAsync(
        int accountId,
        uint state,
        CancellationToken cancellationToken = default);

    Task<DivorceResponse?> RequestDivorceAsync(
        long characterId,
        long partnerCharacterId,
        CancellationToken cancellationToken = default);

    Task<CharacterBanResponse?> RequestCharacterBanAsync(
        int accountId,
        long characterId,
        int durationSeconds,
        CancellationToken cancellationToken = default);

    Task<CharacterUnbanResponse?> RequestCharacterUnbanAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<FameUpdateResponse?> UpdateFameAsync(
        int fameType,
        long characterId,
        int value,
        CancellationToken cancellationToken = default);

    Task<FameListResponse?> RequestFameListAsync(
        int fameType,
        CancellationToken cancellationToken = default);

    Task<BonusScriptGetResponse?> GetBonusScriptAsync(
        long characterId,
        CancellationToken cancellationToken = default);

    Task<BonusScriptSaveResponse?> SaveBonusScriptAsync(
        long characterId,
        byte[] data,
        CancellationToken cancellationToken = default);
    Task<MapAuthConsumeResponse?> ValidateCharAuthTicketAsync(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        CancellationToken cancellationToken = default);

    Task<CharacterDataResponse?> GetCharacterDataAsync(
        long characterId,
        CancellationToken cancellationToken = default);
}
