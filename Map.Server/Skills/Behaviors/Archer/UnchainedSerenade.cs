using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_DISSONANCE — Bard Unchained Serenade (Dissonance). Manual port
/// of <c>rathena-fork/src/map/skills/archer/unchainedserenade.cpp</c>.
///
/// <para>Renewal ratio: <c>+10 + 50*lv</c>, scaled by
/// <c>job_level / 10</c> for players. rAthena's pre-renewal half
/// drops a song ground unit on the targeted cell — that side stays
/// flagged as 🚩 INFRA-DEFERRED on the <see cref="ISkillUnitService"/>
/// pre-renewal lifecycle (song-unit class).</para>
/// </summary>
public sealed class UnchainedSerenade : WeaponSkillImpl
{
    public UnchainedSerenade() : base(SkillIds.BA_DISSONANCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + 10 + skillLevel * 50;
        if (src is PlayerEntity pc)
        {
            ratio = ratio * pc.JobLevel / 10;
        }
        return ratio;
    }
}
