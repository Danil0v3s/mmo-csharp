using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceParty
{
    Task<PartyCreateResponse?> PartyCreateAsync(
        string name,
        int item,
        int item2,
        int leaderAccountId,
        long leaderCharacterId,
        string leaderName,
        int leaderClassId,
        string leaderMapName,
        uint leaderLevel,
        CancellationToken cancellationToken = default);

    Task<PartyInfoResponse?> PartyInfoAsync(
        int partyId,
        long requestingCharacterId,
        CancellationToken cancellationToken = default);

    Task<PartyAddMemberResponse?> PartyAddMemberAsync(
        int partyId,
        int accountId,
        long characterId,
        string name,
        int classId,
        string mapName,
        uint level,
        CancellationToken cancellationToken = default);

    Task<PartyChangeOptionResponse?> PartyChangeOptionAsync(
        int partyId,
        int accountId,
        int exp,
        int item,
        CancellationToken cancellationToken = default);

    Task<PartyLeaveResponse?> PartyLeaveAsync(
        int partyId,
        int accountId,
        long characterId,
        string name,
        int withdrawType,
        CancellationToken cancellationToken = default);

    Task<PartyChangeMapResponse?> PartyChangeMapAsync(
        int partyId,
        int accountId,
        long characterId,
        bool online,
        uint level,
        string mapName,
        CancellationToken cancellationToken = default);

    Task<PartyBreakResponse?> PartyBreakAsync(
        int partyId,
        CancellationToken cancellationToken = default);

    Task<PartyMessageResponse?> PartyMessageAsync(
        int partyId,
        int accountId,
        string message,
        CancellationToken cancellationToken = default);

    Task<PartyLeaderChangeResponse?> PartyLeaderChangeAsync(
        int partyId,
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<PartyShareLevelResponse?> PartyShareLevelAsync(
        uint shareLevel,
        CancellationToken cancellationToken = default);
}
