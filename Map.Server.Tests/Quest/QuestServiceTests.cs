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

    private static QuestMobContext Mob(string aegis, int level = 1, string race = "Formless", string size = "Small", string element = "Neutral")
        => new(1002, aegis, level, race, size, element);

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

        svc.UpdateMobObjective(pc, Mob("LUNATIC")); // wrong mob
        Assert.Equal(0, pc.QuestLog[0].Counts[0]);

        svc.UpdateMobObjective(pc, Mob("PORING"));
        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
        Assert.Equal(2, pc.QuestLog[0].State); // auto-complete
    }

    // --- FEATURE-21: any-mob objective filters (race / size / element / level / location / allow-list) ---

    [Fact]
    public void AnyMob_race_filter_counts_only_matching_race()
    {
        // Mob1 empty (mob_id == 0) → filtered objective: kill 2 Fish-type.
        var (svc, pc) = Build(catalog: new QuestDbEntity { QuestId = 2000, Count1 = 2, Race1 = "Fish" });
        svc.Add(pc, 2000);

        svc.UpdateMobObjective(pc, Mob("MARINA", race: "Plant")); // wrong race
        Assert.Equal(0, pc.QuestLog[0].Counts[0]);

        svc.UpdateMobObjective(pc, Mob("MARINA", race: "Fish")); // matches
        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
    }

    [Fact]
    public void AnyMob_min_level_filter_excludes_low_level_mobs()
    {
        var (svc, pc) = Build(catalog: new QuestDbEntity { QuestId = 2001, Count1 = 5, Race1 = "DemiHuman", MinLevel1 = 140 });
        svc.Add(pc, 2001);

        svc.UpdateMobObjective(pc, Mob("X", level: 100, race: "DemiHuman")); // under MinLevel
        Assert.Equal(0, pc.QuestLog[0].Counts[0]);

        svc.UpdateMobObjective(pc, Mob("X", level: 150, race: "DemiHuman")); // at/above
        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
    }

    [Fact]
    public void AnyMob_size_and_element_filters_apply()
    {
        var (svc, pc) = Build(catalog: new QuestDbEntity { QuestId = 2002, Count1 = 3, Size1 = "Large", Element1 = "Water" });
        svc.Add(pc, 2002);

        svc.UpdateMobObjective(pc, Mob("A", size: "Large", element: "Fire"));  // wrong element
        svc.UpdateMobObjective(pc, Mob("B", size: "Small", element: "Water")); // wrong size
        Assert.Equal(0, pc.QuestLog[0].Counts[0]);

        svc.UpdateMobObjective(pc, Mob("C", size: "Large", element: "Water")); // both match
        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
    }

    [Fact]
    public void AnyMob_allow_list_counts_only_listed_mobs()
    {
        var (svc, pc) = Build(catalog: new QuestDbEntity { QuestId = 2003, Count1 = 4, MobsAllowed1 = "ILL_MUNAK|ILL_SOHEE" });
        svc.Add(pc, 2003);

        svc.UpdateMobObjective(pc, Mob("PORING"));    // not in list
        Assert.Equal(0, pc.QuestLog[0].Counts[0]);

        svc.UpdateMobObjective(pc, Mob("ILL_SOHEE")); // listed
        Assert.Equal(1, pc.QuestLog[0].Counts[0]);
    }

    [Fact]
    public void AnyMob_location_filter_matches_player_map()
    {
        var svc = new QuestService(NullLogger<QuestService>.Instance);
        svc.SeedCatalogForTest(new QuestDbEntity { QuestId = 2004, Count1 = 2, Location1 = "prontera" });
        // Player on prontera (MapId is the name hash, mirroring the production Name2MapId).
        var onPront = new PlayerEntity(1, 1, "P", Guid.NewGuid(), (uint)"prontera".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        var elsewhere = new PlayerEntity(2, 2, "Q", Guid.NewGuid(), (uint)"payon".GetHashCode(), 50, 50) { Hp = 1, MaxHp = 1 };
        svc.Add(onPront, 2004);
        svc.Add(elsewhere, 2004);

        svc.UpdateMobObjective(elsewhere, Mob("ANY")); // wrong map
        Assert.Equal(0, elsewhere.QuestLog[0].Counts[0]);

        svc.UpdateMobObjective(onPront, Mob("ANY"));   // on the quest's location
        Assert.Equal(1, onPront.QuestLog[0].Counts[0]);
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
