using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceQuest
{
    Task<QuestLoadResponse?> QuestLoadAsync(
        long characterId,
        CancellationToken cancellationToken = default);

    Task<QuestSaveResponse?> QuestSaveAsync(
        long characterId,
        IEnumerable<QuestEntryData> quests,
        CancellationToken cancellationToken = default);

    Task<AchievementLoadResponse?> AchievementLoadAsync(
        long characterId,
        CancellationToken cancellationToken = default);

    Task<AchievementSaveResponse?> AchievementSaveAsync(
        long characterId,
        IEnumerable<AchievementEntryData> achievements,
        CancellationToken cancellationToken = default);

    Task<AchievementRewardResponse?> AchievementRewardAsync(
        long characterId,
        int achievementId,
        int itemId,
        int itemAmount,
        string characterName,
        string achievementName,
        CancellationToken cancellationToken = default);
}
