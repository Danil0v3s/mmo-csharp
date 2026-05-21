using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_FROSTNOVA — Wizard Frost Nova. Manual port of
/// <c>rathena-fork/src/map/skills/mage/frostnova.cpp</c>.
///
/// <para>Renewal: same MATK formula as Frost Diver (+<c>10*lv</c>).
/// AOE around the caster; victims roll SC_FREEZE at
/// <c>5*lv + 33 %</c> (player caster) or <c>3*lv + 35 %</c> (mob).
/// Full splash dispatch (map_foreachinshootrange) is TODO — for now we
/// land the freeze attempt on the named target.</para>
/// </summary>
public sealed class FrostNova : SkillImpl
{
    private readonly Random _rng;

    public FrostNova() : base(SkillIds.WZ_FROSTNOVA) => _rng = Random.Shared;

    public FrostNova(Random? rng = null) : base(SkillIds.WZ_FROSTNOVA) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Renewal: identical to MG_FROSTDIVER → +10*lv.
        return baseRatio + 10 * skillLevel;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: full splash dispatch via map_foreachinshootrange.
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = src is PlayerEntity ? skillLevel * 5 + 33 : skillLevel * 3 + 35;
        if (_rng.Next(100) < rate)
            ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 8_000, src);
    }
}
