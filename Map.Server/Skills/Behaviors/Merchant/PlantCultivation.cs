using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// CR_CULTIVATION — Crusader Plant Cultivation (skill.cpp:CR_CULTIVATION
/// arm). Spawns a random plant mob at the cast point. Lv 1 picks from
/// the basic 6 (Red/Blue/Green/Yellow/White/Shining); lv 2 adds Black
/// Mushroom into the pool (rAthena's two-tier rotation).
/// </summary>
public sealed class PlantCultivation : SkillImpl
{
    private const int LifetimeMs = 60_000;
    private readonly Random _rng;

    public PlantCultivation() : base(SkillIds.CR_CULTIVATION) => _rng = Random.Shared;
    public PlantCultivation(Random? rng = null) : base(SkillIds.CR_CULTIVATION) => _rng = rng ?? Random.Shared;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Six base plants at lv 1; lv 2+ adds Black Mushroom.
        var pool = skillLevel >= 2
            ? new[] { MobIds.RedPlant, MobIds.BluePlant, MobIds.GreenPlant,
                      MobIds.YellowPlant, MobIds.WhitePlant, MobIds.ShiningPlant,
                      MobIds.BlackMushroom }
            : new[] { MobIds.RedPlant, MobIds.BluePlant, MobIds.GreenPlant,
                      MobIds.YellowPlant, MobIds.WhitePlant, MobIds.ShiningPlant };
        var classId = pool[_rng.Next(pool.Length)];
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // Cultivated plants are passive — no special AI tag, just the
        // master link so they don't aggro the cultivator's party.
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, classId, x, y,
            MobSpecialAi.None, LifetimeMs);
    }
}
