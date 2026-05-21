using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_NIPELHEIM_REQUIEM — Trouvere Nipelheim Requiem. Manual port of
/// <c>rathena-fork/src/map/skills/archer/nipelheimrequiem.cpp</c>.
///
/// <para>Chorus debuff. Splash victims roll <c>4*lv %</c> SC_CURSE
/// and <c>5*lv %</c> SC_HANDICAPSTATE_DEPRESSION (doubled with a
/// chorus partner). Splash + partner check TODO.</para>
/// </summary>
public sealed class NipelheimRequiem : SkillImpl
{
    private readonly Random _rng;

    public NipelheimRequiem() : base(SkillIds.TR_NIPELHEIM_REQUIEM) => _rng = Random.Shared;

    public NipelheimRequiem(Random? rng = null) : base(SkillIds.TR_NIPELHEIM_REQUIEM) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        if (_rng.Next(100) < 4 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        if (_rng.Next(100) < 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.HandicapstateDepression, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
