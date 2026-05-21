using Map.Server.Skills.Behaviors.Novice;

namespace Map.Server.Tests.Skills.Parity;

/// <summary>
/// T3.3 — first family pass. Twelve skills, picked first for two
/// reasons: (1) smallest directory in <see cref="Map.Server.Skills.Behaviors"/>,
/// so debug spend is bounded; (2) Hyper Novice skills are mostly
/// damage-only or single-target heals — clean test cases for the
/// harness shape itself.
///
/// <para>Each test exercises the relevant resolve hook
/// (<c>CastendDamageId</c> for offensive skills,
/// <c>CastendNoDamageId</c> for heal/buff, <c>CastendPos2</c> for
/// ground units) at skill levels 1 and the family-typical max level
/// (5 for most Novice). The recorded side-effect sequence is diffed
/// against a JSON baseline.</para>
/// </summary>
public class NoviceParityTests
{
    private static SkillExerciser NewExerciser() => new(family: "Novice");

    [Fact]
    public void FirstAid_Lv1()
    {
        var ex = NewExerciser();
        ex.RunNoDamage(ex.Create<FirstAid>(), ex.Caster, 1);
        ex.AssertMatchesBaseline("NV_FIRSTAID", 1);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void HelpAngel(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunNoDamage(ex.Create<HelpAngel>(), ex.Caster, lv);
        ex.AssertMatchesBaseline("HN_HELPS_ANGEL", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void DoubleBowlingBash(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunDamage(ex.Create<DoubleBowlingBash>(), ex.Target, lv);
        ex.AssertMatchesBaseline("HN_DOUBLEBOWLINGBASH", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void HellsDrive(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunDamage(ex.Create<HellsDrive>(), ex.Target, lv);
        ex.AssertMatchesBaseline("HN_HELLS_DRIVE", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void JupitelThunderstorm(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunDamage(ex.Create<JupitelThunderstorm>(), ex.Target, lv);
        ex.AssertMatchesBaseline("HN_JUPITEL_THUNDER_STORM", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void MegaSonicBlow(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunDamage(ex.Create<MegaSonicBlow>(), ex.Target, lv);
        ex.AssertMatchesBaseline("HN_MEGA_SONIC_BLOW", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void NapalmVulcanStrike(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunDamage(ex.Create<NapalmVulcanStrike>(), ex.Target, lv);
        ex.AssertMatchesBaseline("HN_NAPALM_VULCAN_STRIKE", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void ShieldChainRush(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunDamage(ex.Create<ShieldChainRush>(), ex.Target, lv);
        ex.AssertMatchesBaseline("HN_SHIELD_CHAIN_RUSH", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void SpiralPierceMax(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunDamage(ex.Create<SpiralPierceMax>(), ex.Target, lv);
        ex.AssertMatchesBaseline("HN_SPIRAL_PIERCE_MAX", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void GroundGravitation(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunPos2(ex.Create<GroundGravitation>(), 105, 105, lv);
        ex.AssertMatchesBaseline("HN_GROUND_GRAVITATION", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void JackFrostNova(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunPos2(ex.Create<JackFrostNova>(), 105, 105, lv);
        ex.AssertMatchesBaseline("HN_JACK_FROST_NOVA", lv);
    }

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)5)]
    public void MeteorStormBuster(ushort lv)
    {
        var ex = NewExerciser();
        ex.RunPos2(ex.Create<MeteorStormBuster>(), 105, 105, lv);
        ex.AssertMatchesBaseline("HN_METEOR_STORM_BUSTER", lv);
    }
}
