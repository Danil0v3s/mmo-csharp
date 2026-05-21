using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_RAGEBURST — Royal Guard Rage Burst. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/rageburst.cpp</c>.
/// With consumed spirit balls: <c>200*balls + (MaxHP - HP)/100</c>.
/// Without balls: <c>2900 + (MaxHP - HP)</c>. We approximate using the
/// missing-HP delta; spirit-ball plumbing is TODO.
/// </summary>
public sealed class RageBurst : WeaponSkillImpl
{
    public RageBurst() : base(SkillIds.LG_RAGEBURST) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var missingHp = Math.Max(0, src.Stats.MaxHp - src.Stats.Hp);
        return baseRatio + 2900 + missingHp;
    }
}
