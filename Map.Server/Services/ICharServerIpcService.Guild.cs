using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceGuild
{
    Task<GuildCreateResponse?> GuildCreateAsync(
        int accountId,
        string name,
        long masterCharacterId,
        string masterName,
        int masterClassId,
        uint masterLevel,
        CancellationToken cancellationToken = default);

    Task<GuildInfoResponse?> GuildInfoAsync(
        int guildId,
        CancellationToken cancellationToken = default);

    Task<GuildAddMemberResponse?> GuildAddMemberAsync(
        int guildId,
        int accountId,
        long characterId,
        string name,
        int classId,
        uint level,
        CancellationToken cancellationToken = default);

    Task<GuildMasterChangeResponse?> GuildMasterChangeAsync(
        int guildId,
        string masterName,
        CancellationToken cancellationToken = default);

    Task<GuildLeaveResponse?> GuildLeaveAsync(
        int guildId,
        int accountId,
        long characterId,
        int flag,
        string message,
        CancellationToken cancellationToken = default);

    Task<GuildChangeMemberInfoShortResponse?> GuildChangeMemberInfoShortAsync(
        int guildId,
        int accountId,
        long characterId,
        bool online,
        uint level,
        int classId,
        CancellationToken cancellationToken = default);

    Task<GuildBreakResponse?> GuildBreakAsync(
        int guildId,
        CancellationToken cancellationToken = default);

    Task<GuildMessageResponse?> GuildMessageAsync(
        int guildId,
        int accountId,
        string message,
        CancellationToken cancellationToken = default);

    Task<GuildBasicInfoChangeResponse?> GuildBasicInfoChangeAsync(
        int guildId,
        int type,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<GuildMemberInfoChangeResponse?> GuildMemberInfoChangeAsync(
        int guildId,
        int accountId,
        long characterId,
        int type,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<GuildPositionChangeResponse?> GuildPositionChangeAsync(
        int guildId,
        int index,
        GuildPositionInfo position,
        CancellationToken cancellationToken = default);

    Task<GuildSkillUpResponse?> GuildSkillUpAsync(
        int guildId,
        uint skillId,
        int accountId,
        int max,
        CancellationToken cancellationToken = default);

    Task<GuildAllianceResponse?> GuildAllianceAsync(
        int guildId1,
        int guildId2,
        int accountId1,
        int accountId2,
        int flag,
        CancellationToken cancellationToken = default);

    Task<GuildNoticeResponse?> GuildNoticeAsync(
        int guildId,
        string notice1,
        string notice2,
        CancellationToken cancellationToken = default);

    Task<GuildEmblemResponse?> GuildEmblemAsync(
        int guildId,
        int dummy,
        byte[] data,
        CancellationToken cancellationToken = default);

    Task<GuildCastleDataLoadResponse?> GuildCastleDataLoadAsync(
        IEnumerable<int> castleIds,
        CancellationToken cancellationToken = default);

    Task<GuildCastleDataSaveResponse?> GuildCastleDataSaveAsync(
        int castleId,
        int index,
        int value,
        CancellationToken cancellationToken = default);

    Task<GuildEmblemVersionResponse?> GuildEmblemVersionAsync(
        int guildId,
        int version,
        CancellationToken cancellationToken = default);
}
