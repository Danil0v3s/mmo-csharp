using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// CR_CULTIVATION — Crusader Plant Cultivation. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/plantcultivation.cpp</c>.
/// Spawns a random plant mob. mob_once_spawn TODO.
/// </summary>
public sealed class PlantCultivation : SkillImpl
{
    public PlantCultivation() : base(SkillIds.CR_CULTIVATION) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: MOBID_GREEN/RED/YELLOW/WHITE/BLUE/SHINING_PLANT class ids
        // aren't enumerated yet, and IMobSpawnService.SpawnAt won't apply the
        // AI_PLANT idle behavior these require.
    }
}
