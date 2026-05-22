using Map.Server.Status;

namespace Map.Server.Tests.Status;

/// <summary>
/// ST.9-ST.12 — verifies the bulk-backfill registers a handler for
/// every StatusType enum value, with the right flag classification
/// (per StatusFlagDefaults or the conservative fallback).
/// </summary>
public class StatusEffectBulkBackfillTests
{
    [Fact]
    public void EveryStatusTypeValue_HasARegisteredHandler()
    {
        var reg = new StatusEffectRegistry();
        var missing = new List<StatusType>();
        foreach (StatusType type in System.Enum.GetValues<StatusType>())
        {
            if (type == StatusType.None || (short)type < 0) continue;
            if (reg.Get(type) == null) missing.Add(type);
        }
        Assert.True(missing.Count == 0,
            $"{missing.Count} StatusType values still unregistered: " +
            string.Join(", ", missing.Take(10)));
    }

    [Fact]
    public void HandWrittenHandlers_NotOverriddenByBulkBackfill()
    {
        var reg = new StatusEffectRegistry();
        // SC_BLESSING is hand-written above the bulk backfill with
        // OnStart that bumps STR/INT/DEX. The bulk backfill must NOT
        // replace it with a NoOp.
        var handler = reg.Get(StatusType.Blessing)!;
        Assert.NotNull(handler);
        Assert.NotEqual(0, handler.PeriodMs.GetHashCode() ^ handler.OnStart.GetHashCode());
        // Apply Blessing and confirm STR moves.
        var pc = new Map.Server.Entities.PlayerEntity(1, 1, "P", System.Guid.NewGuid(), 1, 0, 0);
        pc.Stats.Str = 10;
        var sc = new StatusChange { Type = StatusType.Blessing, Val1 = 5 };
        handler.OnStart(pc, sc, null);
        Assert.Equal(15, pc.Stats.Str);
    }

    [Fact]
    public void BulkBackfilledSc_PicksUpDefaultFlags()
    {
        var reg = new StatusEffectRegistry();
        // SC_QUAGMIRE has explicit hand-written handler with flags;
        // but pick a less-common SC that should fall through to
        // StatusFlagDefaults / fallback.
        // SC_INVINCIBLE is one rAthena marks as a buff; let's check it.
        var flags = reg.GetEffectiveFlags(StatusType.Spurt);
        Assert.True((flags & ScfFlag.Buff) != 0);
    }

    [Fact]
    public void EnumCountSanity_AtLeast900Handlers()
    {
        var reg = new StatusEffectRegistry();
        var count = 0;
        foreach (StatusType type in System.Enum.GetValues<StatusType>())
        {
            if (type == StatusType.None || (short)type < 0) continue;
            if (reg.Get(type) != null) count++;
        }
        // StatusType has ~997 entries; after the backfill every one
        // should be registered.
        Assert.True(count >= 900,
            $"Expected ≥900 registered SC handlers; got {count}");
    }
}
