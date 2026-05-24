using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_CANNIBALIZE — Summon Flora. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/summonflora.cpp</c>.
/// Spawns one of five flora mobs at the cast XY (Mandragora /
/// Hydra / Flora / Parasite / Geographer keyed by skill level) with
/// AI_FLORA, links it to the caster as master, and arms the cleanup
/// timer. Flora mob spawn helper is not yet ported — TODO.
/// </summary>
public sealed class SummonFlora : SkillImpl
{
    public SummonFlora() : base(SkillIds.AM_CANNIBALIZE) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: MOBID_G_MANDRAGORA/HYDRA/FLORA/PARASITE/GEOGRAPHER + AI_FLORA
        // + master-id linkage + delete-timer aren't surfaced through
        // IMobSpawnService.SpawnAt — would create wild summons otherwise.
    }
}
