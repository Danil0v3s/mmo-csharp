using Map.Server.Achievement;
using Map.Server.Entities;
using Map.Server.Quest;
using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T7.1 — verifies <see cref="IQuestService.SnapshotFor"/> and
/// <see cref="IAchievementService.SnapshotFor"/> serialize the
/// in-memory per-PC log into the gRPC payload, and that the
/// reverse <c>Hydrate</c> path round-trips. Also asserts that
/// <see cref="IntifService.QuestSave"/> / <c>AchievementSave</c>
/// dispatch the snapshot (not <c>Array.Empty</c>) when the
/// services are wired.
/// </summary>
public class IntifSnapshotSerializerTests
{
    [Fact]
    public void QuestService_SnapshotAndHydrate_RoundTripPerObjectiveState()
    {
        var quest = new QuestService(NullLogger<QuestService>.Instance);
        var pc = MakePc(charId: 1);
        pc.QuestLog.Add(new QuestEntry
        {
            QuestId = 2042,
            TimeUnix = 1_700_000_000,
            State = 1, // active
            Counts = new[] { 3, 0, 0 },
        });
        pc.QuestLog.Add(new QuestEntry
        {
            QuestId = 7001,
            TimeUnix = 0,
            State = 2, // complete
            Counts = new[] { 1 },
        });

        var snapshot = quest.SnapshotFor(pc);
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(2042, snapshot[0].QuestId);
        Assert.Equal(1_700_000_000L, snapshot[0].TimeUnix);
        Assert.Equal(1, snapshot[0].State);
        Assert.Equal(new[] { 3, 0, 0 }, snapshot[0].Counts);
        Assert.Equal(7001, snapshot[1].QuestId);
        Assert.Equal(2, snapshot[1].State);

        // Round-trip: re-hydrate into a fresh PC, snapshot again,
        // confirm bit-for-bit equality of the payload shape.
        var pc2 = MakePc(charId: 2);
        quest.Hydrate(pc2, snapshot);
        Assert.Equal(2, pc2.QuestLog.Count);
        Assert.Equal(2042, pc2.QuestLog[0].QuestId);
        Assert.Equal(new[] { 3, 0, 0 }, pc2.QuestLog[0].Counts);
        var snapshot2 = quest.SnapshotFor(pc2);
        Assert.Equal(snapshot.Count, snapshot2.Count);
        Assert.Equal(snapshot[0].QuestId, snapshot2[0].QuestId);
        Assert.Equal(snapshot[0].Counts, snapshot2[0].Counts);
    }

    [Fact]
    public void AchievementService_SnapshotAndHydrate_RoundTripScoreAndCounts()
    {
        var ach = new AchievementService(NullLogger<AchievementService>.Instance);
        var pc = MakePc(charId: 3);
        pc.AchievementLog.Add(new AchievementEntry
        {
            AchievementId = 100,
            CompletedUnix = 1_700_000_500,
            RewardedUnix = 1_700_000_600,
            Score = 50,
            Counts = new[] { 10, 20 },
        });

        var snapshot = ach.SnapshotFor(pc);
        Assert.Single(snapshot);
        Assert.Equal(100, snapshot[0].AchievementId);
        Assert.Equal(1_700_000_500L, snapshot[0].CompletedUnix);
        Assert.Equal(1_700_000_600L, snapshot[0].RewardedUnix);
        Assert.Equal(50, snapshot[0].Score);
        Assert.Equal(new[] { 10, 20 }, snapshot[0].Counts);

        var pc2 = MakePc(charId: 4);
        ach.Hydrate(pc2, snapshot);
        Assert.Single(pc2.AchievementLog);
        Assert.Equal(100, pc2.AchievementLog[0].AchievementId);
        Assert.Equal(50, pc2.AchievementLog[0].Score);
        Assert.Equal(new[] { 10, 20 }, pc2.AchievementLog[0].Counts);
    }

    [Fact]
    public void IntifService_QuestSave_DispatchesNonEmptySnapshotWhenWired()
    {
        var fakeIpc = new RecordingQuestIpc();
        var quest = new QuestService(NullLogger<QuestService>.Instance);
        var pc = MakePc(charId: 5);
        pc.QuestLog.Add(new QuestEntry
        {
            QuestId = 999,
            State = 1,
            Counts = new[] { 7 },
        });

        var intif = new IntifService(
            NullLogger<IntifService>.Instance,
            questIpc: fakeIpc,
            questService: quest);

        Assert.Equal(1, intif.QuestSave(pc));
        Assert.Single(fakeIpc.QuestSavePayloads);
        Assert.Equal(5L, fakeIpc.QuestSavePayloads[0].CharacterId);
        // **Key assertion**: the dispatched payload is no longer
        // empty — T7.1 wires the snapshot through.
        Assert.Single(fakeIpc.QuestSavePayloads[0].Quests);
        Assert.Equal(999, fakeIpc.QuestSavePayloads[0].Quests[0].QuestId);
        Assert.Equal(new[] { 7 }, fakeIpc.QuestSavePayloads[0].Quests[0].Counts);
    }

    [Fact]
    public void IntifService_AchievementSave_DispatchesNonEmptySnapshotWhenWired()
    {
        var fakeIpc = new RecordingQuestIpc();
        var ach = new AchievementService(NullLogger<AchievementService>.Instance);
        var pc = MakePc(charId: 6);
        pc.AchievementLog.Add(new AchievementEntry
        {
            AchievementId = 42,
            Score = 12,
            Counts = new[] { 3 },
        });

        var intif = new IntifService(
            NullLogger<IntifService>.Instance,
            questIpc: fakeIpc,
            achievementService: ach);

        Assert.Equal(1, intif.AchievementSave(pc));
        Assert.Single(fakeIpc.AchievementSavePayloads);
        Assert.Equal(6L, fakeIpc.AchievementSavePayloads[0].CharacterId);
        Assert.Single(fakeIpc.AchievementSavePayloads[0].Achievements);
        Assert.Equal(42, fakeIpc.AchievementSavePayloads[0].Achievements[0].AchievementId);
        Assert.Equal(12, fakeIpc.AchievementSavePayloads[0].Achievements[0].Score);
    }

    private static PlayerEntity MakePc(int charId) =>
        new(charId, charId, $"P{charId}", Guid.NewGuid(), mapId: 1, x: 0, y: 0);

    /// <summary>
    /// Captures the full payload (not just the character id like
    /// IntifQuestWiringTests does) so T7.1 can assert the snapshot
    /// content is actually flowing through.
    /// </summary>
    private sealed class RecordingQuestIpc : ICharServerIpcServiceQuest
    {
        public sealed record QuestSavePayload(long CharacterId,
            IReadOnlyList<Core.Server.IPC.QuestEntryData> Quests);
        public sealed record AchievementSavePayload(long CharacterId,
            IReadOnlyList<Core.Server.IPC.AchievementEntryData> Achievements);

        public List<QuestSavePayload> QuestSavePayloads { get; } = new();
        public List<AchievementSavePayload> AchievementSavePayloads { get; } = new();

        public Task<Core.Server.IPC.QuestLoadResponse?> QuestLoadAsync(long characterId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Server.IPC.QuestLoadResponse?>(null);

        public Task<Core.Server.IPC.QuestSaveResponse?> QuestSaveAsync(long characterId,
            IEnumerable<Core.Server.IPC.QuestEntryData> quests,
            CancellationToken cancellationToken = default)
        {
            QuestSavePayloads.Add(new QuestSavePayload(characterId, quests.ToList()));
            return Task.FromResult<Core.Server.IPC.QuestSaveResponse?>(null);
        }

        public Task<Core.Server.IPC.AchievementLoadResponse?> AchievementLoadAsync(long characterId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Server.IPC.AchievementLoadResponse?>(null);

        public Task<Core.Server.IPC.AchievementSaveResponse?> AchievementSaveAsync(long characterId,
            IEnumerable<Core.Server.IPC.AchievementEntryData> achievements,
            CancellationToken cancellationToken = default)
        {
            AchievementSavePayloads.Add(new AchievementSavePayload(characterId, achievements.ToList()));
            return Task.FromResult<Core.Server.IPC.AchievementSaveResponse?>(null);
        }

        public Task<Core.Server.IPC.AchievementRewardResponse?> AchievementRewardAsync(
            long characterId, int achievementId, int itemId, int itemAmount,
            string characterName, string achievementName,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Server.IPC.AchievementRewardResponse?>(null);
    }
}
