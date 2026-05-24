using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_SILVERSNIPER — Mechanic FAW Silver Sniper
/// (skill.cpp:NC_SILVERSNIPER). Spawns
/// <see cref="MobIds.SilverSniper"/> at the cast cell with AI_FAW
/// + master link + 60 s lifetime. The FAW AI handles the auto-attack
/// branch.
/// </summary>
public sealed class FawSilverSniper : SkillImpl
{
    private const int LifetimeMs = 60_000;

    public FawSilverSniper() : base(SkillIds.NC_SILVERSNIPER) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.SilverSniper, x, y,
            MobSpecialAi.Faw, LifetimeMs);
    }
}
