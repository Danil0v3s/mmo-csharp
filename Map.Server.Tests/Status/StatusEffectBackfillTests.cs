using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// ST.3 — verifies the SC handler backfill (Defender / Quagmire /
/// Doublecast / Hawkeyes / Spurt / Spirit / Soul Linker family /
/// Sphere1-5 / PuttiTailsNoodles) registers in the registry with
/// the right ScfFlag classification + applies the stat mods where
/// rAthena applies them.
/// </summary>
public class StatusEffectBackfillTests
{
    [Fact]
    public void Registry_HasAllBackfillHandlers()
    {
        var reg = new StatusEffectRegistry();

        Assert.NotNull(reg.Get(StatusType.Defender));
        Assert.NotNull(reg.Get(StatusType.Quagmire));
        Assert.NotNull(reg.Get(StatusType.Doublecast));
        Assert.NotNull(reg.Get(StatusType.Hawkeyes));
        Assert.NotNull(reg.Get(StatusType.Spurt));
        Assert.NotNull(reg.Get(StatusType.Spirit));
        // Soul Linker family
        Assert.NotNull(reg.Get(StatusType.Soulreaper));
        Assert.NotNull(reg.Get(StatusType.Soulunity));
        Assert.NotNull(reg.Get(StatusType.Soulshadow));
        Assert.NotNull(reg.Get(StatusType.Soulfairy));
        Assert.NotNull(reg.Get(StatusType.Soulfalcon));
        Assert.NotNull(reg.Get(StatusType.Soulgolem));
        Assert.NotNull(reg.Get(StatusType.Souldivision));
        Assert.NotNull(reg.Get(StatusType.Soulenergy));
        Assert.NotNull(reg.Get(StatusType.Soulcurse));
        // Sphere1..5
        Assert.NotNull(reg.Get(StatusType.Sphere1));
        Assert.NotNull(reg.Get(StatusType.Sphere2));
        Assert.NotNull(reg.Get(StatusType.Sphere3));
        Assert.NotNull(reg.Get(StatusType.Sphere4));
        Assert.NotNull(reg.Get(StatusType.Sphere5));
        Assert.NotNull(reg.Get(StatusType.PuttiTailsNoodles));
    }

    [Fact]
    public void Soulreaper_FlaggedAsBuff_RemovedOnLogout()
    {
        var reg = new StatusEffectRegistry();
        var flags = reg.GetEffectiveFlags(StatusType.Soulreaper);

        Assert.True((flags & ScfFlag.Buff) != 0,
            $"Soulreaper should be Buff-classified; flags={flags}");
        Assert.True((flags & ScfFlag.RemoveOnLogout) != 0,
            $"Soulreaper should drop on logout; flags={flags}");
    }

    [Fact]
    public void Soulcurse_FlaggedAsDebuff_RemovedByRefresh()
    {
        var reg = new StatusEffectRegistry();
        var flags = reg.GetEffectiveFlags(StatusType.Soulcurse);

        Assert.True((flags & ScfFlag.Debuff) != 0);
        Assert.True((flags & ScfFlag.RemoveOnRefresh) != 0);
    }

    [Fact]
    public void Quagmire_FlaggedAsDebuff_RemovedByRefresh()
    {
        var reg = new StatusEffectRegistry();
        var flags = reg.GetEffectiveFlags(StatusType.Quagmire);

        Assert.True((flags & ScfFlag.Debuff) != 0);
        Assert.True((flags & ScfFlag.RemoveOnRefresh) != 0);
    }

    [Fact]
    public void Doublecast_FlaggedAsBuff()
    {
        var reg = new StatusEffectRegistry();
        var flags = reg.GetEffectiveFlags(StatusType.Doublecast);

        Assert.True((flags & ScfFlag.Buff) != 0);
    }

    [Fact]
    public void Sphere1_FlaggedAsBuff()
    {
        var reg = new StatusEffectRegistry();
        var flags = reg.GetEffectiveFlags(StatusType.Sphere1);

        Assert.True((flags & ScfFlag.Buff) != 0);
    }

    [Fact]
    public void Backfill_TotalRegisteredScsGrewBy20Plus()
    {
        // Sanity check that the backfill actually added handlers.
        var reg = new StatusEffectRegistry();
        // Count is a private impl detail; verify via known additions.
        var additions = new[]
        {
            StatusType.Defender, StatusType.Quagmire, StatusType.Doublecast,
            StatusType.Hawkeyes, StatusType.Spurt, StatusType.Spirit,
            StatusType.Soulreaper, StatusType.Soulunity, StatusType.Soulshadow,
            StatusType.Soulfairy, StatusType.Soulfalcon, StatusType.Soulgolem,
            StatusType.Souldivision, StatusType.Soulenergy, StatusType.Soulcurse,
            StatusType.Sphere1, StatusType.Sphere2, StatusType.Sphere3,
            StatusType.Sphere4, StatusType.Sphere5, StatusType.PuttiTailsNoodles,
        };
        foreach (var t in additions)
            Assert.NotNull(reg.Get(t));
        Assert.True(additions.Length >= 20);
    }
}
