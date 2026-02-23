using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<QuestLoadResponse?> QuestLoadAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.QuestLoadAsync(new QuestLoadRequest
        {
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<QuestSaveResponse?> QuestSaveAsync(
        long characterId,
        IEnumerable<QuestEntryData> quests,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        var request = new QuestSaveRequest
        {
            CharacterId = characterId
        };
        if (quests != null)
        {
            request.Quests.AddRange(quests);
        }

        return await client.QuestSaveAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<AchievementLoadResponse?> AchievementLoadAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AchievementLoadAsync(new AchievementLoadRequest
        {
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AchievementSaveResponse?> AchievementSaveAsync(
        long characterId,
        IEnumerable<AchievementEntryData> achievements,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        var request = new AchievementSaveRequest
        {
            CharacterId = characterId
        };
        if (achievements != null)
        {
            request.Achievements.AddRange(achievements);
        }

        return await client.AchievementSaveAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<AchievementRewardResponse?> AchievementRewardAsync(
        long characterId,
        int achievementId,
        int itemId,
        int itemAmount,
        string characterName,
        string achievementName,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.AchievementRewardAsync(new AchievementRewardRequest
        {
            CharacterId = characterId,
            AchievementId = achievementId,
            ItemId = itemId,
            ItemAmount = itemAmount,
            CharacterName = characterName ?? string.Empty,
            AchievementName = achievementName ?? string.Empty
        }, cancellationToken: cancellationToken);
    }
}
