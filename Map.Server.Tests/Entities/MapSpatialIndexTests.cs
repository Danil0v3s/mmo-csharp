using Map.Server.Entities;

namespace Map.Server.Tests.Entities;

public class MapSpatialIndexTests
{
    [Fact]
    public void Insert_Then_ForEachInRange_ReturnsEntity()
    {
        var idx = new MapSpatialIndex(100, 100);
        var id = new EntityId(42);

        idx.Insert(id, 50, 50);

        var hits = idx.ForEachInRange(50, 50, 0);
        Assert.Single(hits);
        Assert.Equal(id, hits[0]);
    }

    [Fact]
    public void ForEachInRange_RangeZero_OnlyReturnsCentreCell()
    {
        var idx = new MapSpatialIndex(100, 100);
        idx.Insert(new EntityId(1), 50, 50);
        idx.Insert(new EntityId(2), 51, 50);

        var hits = idx.ForEachInRange(50, 50, range: 0);
        Assert.Single(hits);
        Assert.Equal(new EntityId(1), hits[0]);
    }

    [Fact]
    public void ForEachInRange_BoundingBoxRespectsRange()
    {
        var idx = new MapSpatialIndex(100, 100);
        idx.Insert(new EntityId(1), 50, 50); // centre
        idx.Insert(new EntityId(2), 64, 50); // exactly +14
        idx.Insert(new EntityId(3), 65, 50); // +15 — outside

        var hits = idx.ForEachInRange(50, 50, range: 14);
        Assert.Equal(2, hits.Count);
        Assert.Contains(new EntityId(1), hits);
        Assert.Contains(new EntityId(2), hits);
        Assert.DoesNotContain(new EntityId(3), hits);
    }

    [Fact]
    public void ForEachInRange_ClampsToMapEdges()
    {
        var idx = new MapSpatialIndex(10, 10);
        idx.Insert(new EntityId(1), 0, 0);
        idx.Insert(new EntityId(2), 9, 9);

        // Range query straddling the SW corner — should clip the lower bound to 0.
        var hits = idx.ForEachInRange(0, 0, range: 5);
        Assert.Single(hits);
        Assert.Equal(new EntityId(1), hits[0]);
    }

    [Fact]
    public void Remove_DropsEntityFromBucket()
    {
        var idx = new MapSpatialIndex(10, 10);
        var id = new EntityId(7);
        idx.Insert(id, 5, 5);
        Assert.Single(idx.ForEachInRange(5, 5, 0));

        idx.Remove(id, 5, 5);
        Assert.Empty(idx.ForEachInRange(5, 5, 0));
    }

    [Fact]
    public void Move_TransfersEntityBetweenBuckets()
    {
        var idx = new MapSpatialIndex(20, 20);
        var id = new EntityId(99);
        idx.Insert(id, 1, 1);

        idx.Move(id, 1, 1, 10, 10);

        Assert.Empty(idx.ForEachInRange(1, 1, 0));
        Assert.Single(idx.ForEachInRange(10, 10, 0));
    }

    [Fact]
    public void ForEachInArea_Inclusive_AndClamped()
    {
        var idx = new MapSpatialIndex(10, 10);
        idx.Insert(new EntityId(1), 0, 0);
        idx.Insert(new EntityId(2), 5, 5);
        idx.Insert(new EntityId(3), 9, 9);

        // x0/y0 negative get clamped to 0; x1/y1 over the edge clamp down.
        var hits = idx.ForEachInArea(-5, -5, 15, 15);
        Assert.Equal(3, hits.Count);
    }

    [Fact]
    public void OutOfBounds_InsertIsNoOp()
    {
        var idx = new MapSpatialIndex(5, 5);
        idx.Insert(new EntityId(1), -1, 0);
        idx.Insert(new EntityId(2), 0, 5);
        idx.Insert(new EntityId(3), 5, 0);
        // None should land in any cell; entire-area scan returns empty.
        Assert.Empty(idx.ForEachInArea(0, 0, 4, 4));
    }
}
