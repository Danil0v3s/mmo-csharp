using System;
using Map.Server.Entities;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Acolyte;
using Map.Server.Status;
using Map.Server.Tests.Skills.Parity;

namespace Map.Server.Tests.Skills;

/// <summary>
/// COMBAT-68 — pins the renewal HP_BASILICA behavior after the premise correction. Renewal
/// Basilica is the <see cref="StatusType.Basilica"/> self-buff only: it places no ground unit
/// and never applies <see cref="StatusType.BasilicaCell"/> (the cell-marking that feeds it,
/// skill.cpp:21830, is <c>#ifndef RENEWAL</c>). The COMBAT-49 SC_BASILICA_CELL damage-immunity
/// is therefore inert in renewal by design (it only fires for a hand-applied/scripted SC). The
/// renewal offensive element buff + NoAttack state are tracked in COMBAT-87.
/// </summary>
public class Combat68BasilicaRenewalTests
{
    [Fact]
    public void HP_BASILICA_applies_the_self_buff_not_the_cell_immunity()
    {
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var ctx = new SkillBehaviorContext(null!, null!, null!, sc, Client: null);
        var caster = new PlayerEntity(1, 1, "Priest", Guid.NewGuid(), 0, 50, 50);

        new Basilica().CastendNoDamageId(caster, caster, skillLevel: 3, ctx);

        // Renewal: SC_BASILICA on the caster (val1 = level), NOT SC_BASILICA_CELL.
        var buff = sc.Get(caster, StatusType.Basilica);
        Assert.NotNull(buff);
        Assert.Equal(3, buff!.Val1);
        Assert.Null(sc.Get(caster, StatusType.BasilicaCell));
    }

    [Fact]
    public void CastendPos2_places_no_ground_unit_in_renewal()
    {
        // The renewal cast has no ground-unit placement; CastendPos2 is a no-op (no _units
        // dependency, nothing to assert beyond "does not throw / applies no SC").
        var sc = new RecordingStatusChangeService(new SkillTraceRecorder());
        var ctx = new SkillBehaviorContext(null!, null!, null!, sc, Client: null);
        var caster = new PlayerEntity(1, 1, "Priest", Guid.NewGuid(), 0, 50, 50);

        new Basilica().CastendPos2(caster, 50, 50, skillLevel: 1, ctx);

        Assert.Null(sc.Get(caster, StatusType.Basilica));
        Assert.Null(sc.Get(caster, StatusType.BasilicaCell));
    }
}
