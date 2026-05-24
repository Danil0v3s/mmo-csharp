using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CANNIBALIZE — Summon Flora (skill.cpp:AM_CANNIBALIZE arm).
/// Spawns one of five flora mobs keyed by skill level (rAthena
/// skill.cpp:11825): lv 1 → Mandragora, lv 2 → Hydra, lv 3 → Flora,
/// lv 4 → Parasite, lv 5 → Geographer. Each is tagged
/// <see cref="MobSpecialAi.Flora"/> + bound to the caster as master
/// with a 60 s lifetime cap.
/// </summary>
public sealed class SummonFlora : SkillImpl
{
    private const int LifetimeMs = 60_000;

    public SummonFlora() : base(SkillIds.AM_CANNIBALIZE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var classId = skillLevel switch
        {
            1 => MobIds.GMandragora,
            2 => MobIds.GHydra,
            3 => MobIds.GFlora,
            4 => MobIds.GParasite,
            _ => MobIds.GGeographer, // lv 5+
        };
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, classId, x, y,
            MobSpecialAi.Flora, LifetimeMs);
    }
}
