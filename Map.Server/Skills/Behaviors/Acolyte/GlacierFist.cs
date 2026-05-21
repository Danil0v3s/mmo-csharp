using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CH_TIGERFIST — Champion Glacier Fist / Tiger Fist. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/glacierfist.cpp</c>.
///
/// <para>Renewal ratio: <c>+400 + 150*lv</c>. On hit applies
/// SC_ANKLE (Ankle Snare equivalent) at <c>(1+lv)*10 %</c> chance
/// with duration scaling on target AGI.</para>
/// </summary>
public sealed class GlacierFist : WeaponSkillImpl
{
    private readonly Random _rng;

    public GlacierFist() : base(SkillIds.CH_TIGERFIST) => _rng = Random.Shared;

    public GlacierFist(Random? rng = null) : base(SkillIds.CH_TIGERFIST) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 400 + 150 * skillLevel;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // basetime = skill_get_time(...) — baseline ~5000ms; reduced /5 for status-immune.
        long basetime = 5000;
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0) basetime /= 5;
        // Scale by target AGI: basetime * AGI / -200 + basetime. Mintime = 15 * (lv+100).
        long mintime = 15 * (src.Level + 100);
        basetime = Math.Max((basetime * target.Stats.Agi) / -200 + basetime, mintime);

        var chance = (1 + skillLevel) * 10;
        if (_rng.Next(100) < chance)
        {
            ctx.Sc?.Start(target, StatusType.Ankle, val1: 0, 0, 0, 0, (int)basetime, src);
        }
    }
}
