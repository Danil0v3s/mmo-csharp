using Core.Database.Entities;
using Map.Server.Entities;
using Map.Server.Quest;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Quest;

/// <summary>
/// FEATURE-03 — quest service state machine (add/delete/change/check/objective/expiry).
/// </summary>
public class QuestServiceTests
{
    private static (QuestService svc, PlayerEntity pc) Build(DateTimeOffset? now = null, params QuestDbEntity[] catalog)
    {
        var svc = new QuestService(NullLogger<QuestService>.Instance);
        svc.SeedCatalogForTest(catalog);
        if (now is { } n) svc.Clock = () => n;
        var pc = new PlayerEntity(1, 1, "P1", Guid.NewGuid(), 1, 50, 50) { Hp = 1000, MaxHp = 1000 };
        return (svc, pc);
    }

    private static QuestDbEntity Quest(uint id, string mob = "PORING", int count = 2, string time = "")
        => new() { QuestId = id, Mob1 = mob, Count1 = count, TimeLimit = time };

    // --- Add / Delete / Change ---

    [Fact]
    public void Add_then_HaveQuest_is_active_then_Delete_is_absent()
    {
        var (svc, pc) = Build(catalog: Quest(1000));

        Assert.Equal(0, svc.Add(pc, 1000));
        Assert.Equal((int)1, svc.Check(pc, 1000, (byte)QuestCheckType.HaveQuest)); // Q_ACTIVE
        Assert.Single(pc.QuestLog);

        Assert.Equal(0, svc.Delete(pc, 1000));
        Assert.Equal(-1, svc.Check(pc, 1000, (byte)QuestCheckType.HaveQuest));
        Assert.Empty(pc.QuestLog);
    }

    [Fact]
    public void Add_duplicate_and_unknown_fail()
    {
        var (svc, pc) = Build(catalog: Quest(1000));
        Assert.Equal(0, svc.Add(pc, 1000));
        Assert.Equal(-1, svc.Add(pc, 1000));  // already has it
        Assert.Equal(-1, svc.Add(pc, 9999));  // not in catalog
    }

    [Fact]
    public void Delete_absent_returns_minus1()
    {
        var (svc, pc) = Build(catalog: Quest(1000));
        Assert.Equal(-1, svc.Delete(pc, 1000));
    }

    [Fact]
    public void Change_replaces_in_same_slot()
    {
        var (svc, pc) = Build();
        svc.SeedCatalogForTest(Quest(1000), Quest(1001));
        Assert.Equal(0, svc.Add(pc, 1000));

        Assert.Equal(0, svc.Change(pc, 1000, 1001));
        Assert.Equal(-1, svc.Check(pc, 1000, (byte)QuestCheckType.HaveQuest));
        Assert.Equal(1, svc.Check(pc, 1001, (byte)QuestCheckType.HaveQuest));
        Assert.Single(pc.QuestLog);
        Assert.Equal(1001, pc.QuestLog[0].QuestId);
    }

    [Fact]
    public void Change_fails_without_old_or_with_existing_new()
    {
        var (svc, pc) = Build();
        svc.SeedCatalogForTest(Quest(1000), Quest(1001));
        Assert.Equal(-1, svc.Change(pc, 1000, 1001)); // lacks old
        svc.Add(pc, 1000);
        svc.Add(pc, 1001);
        Assert.Equal(-1, svc.Change(pc, 1000, 1001)); // already has new
    }

    // --- Objectives ---

    [Fact]
    public void UpdateObjective_increments_clamps_and_completes()
    {
        var (svc, pc) = Build(catalog: Quest(1000, count: 2));
        svc.Add(pc, 1000);

        svc.UpdateObjective(pc, 1000, 0, 1);
        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
        Assert.Equal(1, pc.QuestLog[0].State); // active

        svc.UpdateObjective(pc, 1000, 0, 1);
        Assert.Equal(2, pc.QuestLog[0].Counts[0]);
        Assert.Equal(2, pc.QuestLog[0].State); // complete

        svc.UpdateObjective(pc, 1000, 0, 1); // clamp — no further change
        Assert.Equal(2, pc.QuestLog[0].Counts[0]);
    }

    [Fact]
    public void UpdateMobObjective_matches_by_aegis()
    {
        var (svc, pc) = Build(catalog: Quest(1000, mob: "PORING", count: 1));
        svc.Add(pc, 1000);

        svc.UpdateMobObjective(pc, "LUNATIC"); // wrong mob
        Assert.Equal(0, pc.QuestLog[0].Counts[0]);

        svc.UpdateMobObjective(pc, "PORING");
        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
        Assert.Equal(2, pc.QuestLog[0].State); // auto-complete
    }

    // --- Check query codes ---

    [Fact]
    public void Check_PlayTime_reports_expiry_and_completion()
    {
        var now = new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero);
        var (svc, pc) = Build(now, Quest(1000, time: "+1h"));
        svc.Add(pc, 1000);

        Assert.Equal(0, svc.Check(pc, 1000, (byte)QuestCheckType.PlayTime)); // not expired, not complete

        svc.Clock = () => now.AddHours(2); // past the +1h window
        Assert.Equal(2, svc.Check(pc, 1000, (byte)QuestCheckType.PlayTime)); // expired
    }

    [Fact]
    public void Check_PlayTime_complete_returns_1()
    {
        var (svc, pc) = Build(catalog: Quest(1000, count: 1, time: "")); // no time limit
        svc.Add(pc, 1000);
        svc.UpdateObjective(pc, 1000, 0, 1); // completes it

        Assert.Equal(1, svc.Check(pc, 1000, (byte)QuestCheckType.PlayTime));
    }

    [Fact]
    public void Check_Hunting_reports_objectives_and_expiry()
    {
        var now = new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero);
        var (svc, pc) = Build(now, Quest(1000, count: 1, time: "+1h"));
        svc.Add(pc, 1000);

        Assert.Equal(0, svc.Check(pc, 1000, (byte)QuestCheckType.Hunting)); // not met, not expired

        svc.Clock = () => now.AddHours(2);
        Assert.Equal(1, svc.Check(pc, 1000, (byte)QuestCheckType.Hunting)); // expired before completion

        svc.Clock = () => now;
        svc.UpdateObjective(pc, 1000, 0, 1); // meet the objective (auto-completes)
        // Hunting only reports 2 while still active/inactive; a completed quest reports 0.
        var q = pc.QuestLog[0];
        q.State = 1; // force active to observe the "objectives met" branch
        Assert.Equal(2, svc.Check(pc, 1000, (byte)QuestCheckType.Hunting));
    }

    [Fact]
    public void Add_no_timelimit_never_expires()
    {
        var (svc, pc) = Build(catalog: Quest(1000, time: ""));
        svc.Add(pc, 1000);
        Assert.Equal(0, pc.QuestLog[0].TimeUnix);
        svc.Clock = () => DateTimeOffset.UtcNow.AddYears(5);
        Assert.Equal(0, svc.Check(pc, 1000, (byte)QuestCheckType.PlayTime)); // 0 = not expired
    }

    [Fact]
    public void PcLogin_returns_active_quest_count()
    {
        var (svc, pc) = Build();
        svc.SeedCatalogForTest(Quest(1000, count: 1), Quest(1001));
        svc.Add(pc, 1000);
        svc.Add(pc, 1001);
        svc.UpdateObjective(pc, 1000, 0, 1); // completes 1000

        Assert.Equal(1, svc.PcLogin(pc)); // only 1001 still active
    }
}
