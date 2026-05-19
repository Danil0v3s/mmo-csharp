using Core.Database.Entities;
using Map.Server.Skills;
using Map.Server.Status;

namespace Map.Server.Tests.Skills;

public class SkillDbLoaderTests
{
    [Fact]
    public void FromEntity_RoundTrips_KeyColumns()
    {
        var row = new SkillDbEntity
        {
            Id = 5, Name = "Bash", MaxLevel = 10,
            TargetMode = "TargetEnemy", DamageKind = "Weapon",
            Range = 1, Element = "Neutral", StatusType = "None",
            SpCost = "8:8:8:8:8:15:15:15:15:15",
            DamageRate = "130:160:190:220:250:280:310:340:370:400",
        };

        var def = SkillDbLoader.FromEntity(row);

        Assert.Equal(5, def.Id);
        Assert.Equal((ushort)10, def.MaxLevel);
        Assert.Equal(SkillTargetMode.TargetEnemy, def.Target);
        Assert.Equal(SkillDamageKind.Weapon, def.DamageKind);
        Assert.Equal(BattleElement.Neutral, def.Element);
        // SpCost: 0 (unused index) + 10 levels parsed.
        Assert.Equal(11, def.SpCost.Length);
        Assert.Equal(8, def.SpCost[1]);
        Assert.Equal(8, def.SpCost[5]);
        Assert.Equal(15, def.SpCost[6]);
        Assert.Equal(15, def.SpCost[10]);
        // DamageRate parsed.
        Assert.Equal(130, def.DamageRate[1]);
        Assert.Equal(400, def.DamageRate[10]);
    }

    [Fact]
    public void FromEntity_EmptyPackedColumn_GivesZeroFilledArray()
    {
        var row = new SkillDbEntity
        {
            Id = 1, Name = "Empty", MaxLevel = 3,
            TargetMode = "SelfOnly", DamageKind = "None",
            SpCost = string.Empty,
        };
        var def = SkillDbLoader.FromEntity(row);
        Assert.Equal(4, def.SpCost.Length); // 0 + 3 levels
        Assert.All(def.SpCost, c => Assert.Equal(0, c));
    }

    [Fact]
    public void FromEntity_StatusType_Parsed()
    {
        var row = new SkillDbEntity
        {
            Id = 29, Name = "Increase AGI", MaxLevel = 10,
            TargetMode = "TargetFriend", DamageKind = "None",
            StatusType = "IncreaseAgi",
            EffectAmount = "1:2:3:4:5:6:7:8:9:10",
            StatusDurationMs = "50000:60000:70000:80000:90000:100000:110000:120000:130000:140000",
        };
        var def = SkillDbLoader.FromEntity(row);
        Assert.Equal(StatusType.IncreaseAgi, def.StatusType);
        Assert.Equal(10, def.EffectAmount[10]);
        Assert.Equal(140000, def.StatusDurationMs[10]);
    }
}
