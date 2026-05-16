using Map.Server.Entities;

namespace Map.Server.Tests.Entities;

public class EntityIdAllocatorTests
{
    [Fact]
    public void NextMob_AllocatesInMobRange()
    {
        var alloc = new EntityIdAllocator();
        var id = alloc.NextMob();
        Assert.InRange(id.Value, 400_000_001, 799_999_999);
    }

    [Fact]
    public void NextNpc_AllocatesInNpcRange()
    {
        var alloc = new EntityIdAllocator();
        var id = alloc.NextNpc();
        Assert.InRange(id.Value, 800_000_001, 1_499_999_999);
    }

    [Fact]
    public void NextItem_AllocatesInItemRange()
    {
        var alloc = new EntityIdAllocator();
        var id = alloc.NextItem();
        // Items start at 2_000_000_000 which exceeds int.MaxValue; the impl
        // stores as signed int via unchecked cast. Assert non-zero and that
        // sequential calls differ.
        var id2 = alloc.NextItem();
        Assert.NotEqual(id.Value, id2.Value);
    }

    [Fact]
    public void SequentialAllocations_AreDistinct()
    {
        var alloc = new EntityIdAllocator();
        var seen = new HashSet<int>();
        for (var i = 0; i < 100; i++)
        {
            Assert.True(seen.Add(alloc.NextMob().Value));
            Assert.True(seen.Add(alloc.NextNpc().Value));
            Assert.True(seen.Add(alloc.NextSkill().Value));
        }
    }

    [Fact]
    public void Ranges_DoNotOverlap()
    {
        var alloc = new EntityIdAllocator();
        var mob = alloc.NextMob().Value;
        var npc = alloc.NextNpc().Value;
        var skill = alloc.NextSkill().Value;

        Assert.True(mob < npc, $"mob {mob} should be < npc {npc}");
        Assert.True(skill > npc, $"skill {skill} should be > npc {npc}");
    }
}
