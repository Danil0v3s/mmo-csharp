using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T5.4b + T5.4c — verifies IntifService dispatches Quest + Achievement
/// load/save RPCs through ICharServerIpcServiceQuest when wired, and
/// short-circuits to 0 otherwise.
/// </summary>
public class IntifQuestWiringTests
{
    [Fact]
    public void QuestSave_WithoutIpc_ReturnsZero()
    {
        var intif = new IntifService(NullLogger<IntifService>.Instance);
        var pc = MakePc(charId: 1);
        Assert.Equal(0, intif.QuestSave(pc));
    }

    [Fact]
    public void QuestSave_WithIpc_Dispatches()
    {
        var fake = new RecordingQuestIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, questIpc: fake);
        var pc = MakePc(charId: 7);

        Assert.Equal(1, intif.QuestSave(pc));
        Assert.Single(fake.QuestSaveCalls);
        Assert.Equal(7L, fake.QuestSaveCalls[0].CharacterId);
    }

    [Fact]
    public void QuestRequest_WithIpc_Dispatches()
    {
        var fake = new RecordingQuestIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, questIpc: fake);

        Assert.Equal(1, intif.QuestRequest(charId: 7));
        Assert.Single(fake.QuestLoadCalls);
        Assert.Equal(7L, fake.QuestLoadCalls[0]);
    }

    [Fact]
    public void AchievementSave_WithIpc_Dispatches()
    {
        var fake = new RecordingQuestIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, questIpc: fake);
        var pc = MakePc(charId: 7);

        Assert.Equal(1, intif.AchievementSave(pc));
        Assert.Single(fake.AchievementSaveCalls);
        Assert.Equal(7L, fake.AchievementSaveCalls[0]);
    }

    [Fact]
    public void AchievementRequest_WithIpc_Dispatches()
    {
        var fake = new RecordingQuestIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, questIpc: fake);

        Assert.Equal(1, intif.AchievementRequest(charId: 7));
        Assert.Single(fake.AchievementLoadCalls);
        Assert.Equal(7L, fake.AchievementLoadCalls[0]);
    }

    private static PlayerEntity MakePc(int charId) =>
        new(charId, charId, $"P{charId}", Guid.NewGuid(), mapId: 1, x: 0, y: 0);

    private sealed class RecordingQuestIpc : ICharServerIpcServiceQuest
    {
        public sealed record QuestSaveCall(long CharacterId, int Count);
        public List<QuestSaveCall> QuestSaveCalls { get; } = new();
        public List<long> QuestLoadCalls { get; } = new();
        public List<long> AchievementSaveCalls { get; } = new();
        public List<long> AchievementLoadCalls { get; } = new();

        public Task<Core.Server.IPC.QuestLoadResponse?> QuestLoadAsync(long characterId,
            CancellationToken cancellationToken = default)
        {
            QuestLoadCalls.Add(characterId);
            return Task.FromResult<Core.Server.IPC.QuestLoadResponse?>(null);
        }

        public Task<Core.Server.IPC.QuestSaveResponse?> QuestSaveAsync(long characterId,
            IEnumerable<Core.Server.IPC.QuestEntryData> quests,
            CancellationToken cancellationToken = default)
        {
            QuestSaveCalls.Add(new QuestSaveCall(characterId, quests?.Count() ?? 0));
            return Task.FromResult<Core.Server.IPC.QuestSaveResponse?>(null);
        }

        public Task<Core.Server.IPC.AchievementLoadResponse?> AchievementLoadAsync(long characterId,
            CancellationToken cancellationToken = default)
        {
            AchievementLoadCalls.Add(characterId);
            return Task.FromResult<Core.Server.IPC.AchievementLoadResponse?>(null);
        }

        public Task<Core.Server.IPC.AchievementSaveResponse?> AchievementSaveAsync(long characterId,
            IEnumerable<Core.Server.IPC.AchievementEntryData> achievements,
            CancellationToken cancellationToken = default)
        {
            AchievementSaveCalls.Add(characterId);
            return Task.FromResult<Core.Server.IPC.AchievementSaveResponse?>(null);
        }

        public Task<Core.Server.IPC.AchievementRewardResponse?> AchievementRewardAsync(
            long characterId, int achievementId, int itemId, int itemAmount,
            string characterName, string achievementName,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Server.IPC.AchievementRewardResponse?>(null);
    }
}
