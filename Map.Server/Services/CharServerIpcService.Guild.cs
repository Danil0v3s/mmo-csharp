using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<GuildCreateResponse?> GuildCreateAsync(
        int accountId,
        string name,
        long masterCharacterId,
        string masterName,
        int masterClassId,
        uint masterLevel,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildCreateAsync(new GuildCreateRequest
        {
            AccountId = accountId,
            Name = name ?? string.Empty,
            MasterCharacterId = masterCharacterId,
            MasterName = masterName ?? string.Empty,
            MasterClassId = masterClassId,
            MasterLevel = masterLevel
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildInfoResponse?> GuildInfoAsync(
        int guildId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildInfoAsync(new GuildInfoRequest
        {
            GuildId = guildId
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildAddMemberResponse?> GuildAddMemberAsync(
        int guildId,
        int accountId,
        long characterId,
        string name,
        int classId,
        uint level,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildAddMemberAsync(new GuildAddMemberRequest
        {
            GuildId = guildId,
            AccountId = accountId,
            CharacterId = characterId,
            Name = name ?? string.Empty,
            ClassId = classId,
            Level = level
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildMasterChangeResponse?> GuildMasterChangeAsync(
        int guildId,
        string masterName,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildMasterChangeAsync(new GuildMasterChangeRequest
        {
            GuildId = guildId,
            MasterName = masterName ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildLeaveResponse?> GuildLeaveAsync(
        int guildId,
        int accountId,
        long characterId,
        int flag,
        string message,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildLeaveAsync(new GuildLeaveRequest
        {
            GuildId = guildId,
            AccountId = accountId,
            CharacterId = characterId,
            Flag = flag,
            Message = message ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildChangeMemberInfoShortResponse?> GuildChangeMemberInfoShortAsync(
        int guildId,
        int accountId,
        long characterId,
        bool online,
        uint level,
        int classId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildChangeMemberInfoShortAsync(new GuildChangeMemberInfoShortRequest
        {
            GuildId = guildId,
            AccountId = accountId,
            CharacterId = characterId,
            Online = online,
            Level = level,
            ClassId = classId
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildBreakResponse?> GuildBreakAsync(
        int guildId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildBreakAsync(new GuildBreakRequest
        {
            GuildId = guildId
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildMessageResponse?> GuildMessageAsync(
        int guildId,
        int accountId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildMessageAsync(new GuildMessageRequest
        {
            GuildId = guildId,
            AccountId = accountId,
            Message = message ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildBasicInfoChangeResponse?> GuildBasicInfoChangeAsync(
        int guildId,
        int type,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildBasicInfoChangeAsync(new GuildBasicInfoChangeRequest
        {
            GuildId = guildId,
            Type = type,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildMemberInfoChangeResponse?> GuildMemberInfoChangeAsync(
        int guildId,
        int accountId,
        long characterId,
        int type,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildMemberInfoChangeAsync(new GuildMemberInfoChangeRequest
        {
            GuildId = guildId,
            AccountId = accountId,
            CharacterId = characterId,
            Type = type,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildPositionChangeResponse?> GuildPositionChangeAsync(
        int guildId,
        int index,
        GuildPositionInfo position,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildPositionChangeAsync(new GuildPositionChangeRequest
        {
            GuildId = guildId,
            Index = index,
            Position = position ?? new GuildPositionInfo()
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildSkillUpResponse?> GuildSkillUpAsync(
        int guildId,
        uint skillId,
        int accountId,
        int max,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildSkillUpAsync(new GuildSkillUpRequest
        {
            GuildId = guildId,
            SkillId = skillId,
            AccountId = accountId,
            Max = max
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildAllianceResponse?> GuildAllianceAsync(
        int guildId1,
        int guildId2,
        int accountId1,
        int accountId2,
        int flag,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildAllianceAsync(new GuildAllianceRequest
        {
            GuildId1 = guildId1,
            GuildId2 = guildId2,
            AccountId1 = accountId1,
            AccountId2 = accountId2,
            Flag = flag
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildNoticeResponse?> GuildNoticeAsync(
        int guildId,
        string notice1,
        string notice2,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildNoticeAsync(new GuildNoticeRequest
        {
            GuildId = guildId,
            Notice1 = notice1 ?? string.Empty,
            Notice2 = notice2 ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildEmblemResponse?> GuildEmblemAsync(
        int guildId,
        int dummy,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildEmblemAsync(new GuildEmblemRequest
        {
            GuildId = guildId,
            Dummy = dummy,
            Data = Google.Protobuf.ByteString.CopyFrom(data ?? Array.Empty<byte>())
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildCastleDataLoadResponse?> GuildCastleDataLoadAsync(
        IEnumerable<int> castleIds,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        var request = new GuildCastleDataLoadRequest();
        request.CastleIds.AddRange(castleIds);
        return await client.GuildCastleDataLoadAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GuildCastleDataSaveResponse?> GuildCastleDataSaveAsync(
        int castleId,
        int index,
        int value,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildCastleDataSaveAsync(new GuildCastleDataSaveRequest
        {
            CastleId = castleId,
            Index = index,
            Value = value
        }, cancellationToken: cancellationToken);
    }

    public async Task<GuildEmblemVersionResponse?> GuildEmblemVersionAsync(
        int guildId,
        int version,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.GuildEmblemVersionAsync(new GuildEmblemVersionRequest
        {
            GuildId = guildId,
            Version = version
        }, cancellationToken: cancellationToken);
    }
}
