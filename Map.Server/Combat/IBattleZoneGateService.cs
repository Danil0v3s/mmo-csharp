using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// BG / GvG friendly-fire gates + the mob-AI exception list.
/// rAthena reference points:
///
/// <list type="bullet">
///   <item><c>battle_can_hit_bg_target</c> (battle.cpp:9510) —
///         within a BG round, allies on the same team can't
///         damage each other.</item>
///   <item><c>battle_can_hit_gvg_target</c> (battle.cpp:9555) —
///         within a GvG zone, allies of the same guild / ally
///         guild can't damage each other unless
///         <c>gvg_traps_target_all</c> is set.</item>
///   <item><c>battle_get_exception_ai</c> (battle.cpp:9605) —
///         mob_db mode lookup for AI exceptions (mobs that
///         ignore standard target rules).</item>
/// </list>
/// </summary>
public interface IBattleZoneGateService
{
    /// <summary>True if <paramref name="src"/> can damage <paramref name="target"/> in a BG.</summary>
    bool CanHitBgTarget(Entity src, Entity target);

    /// <summary>True if <paramref name="src"/> can damage <paramref name="target"/> in GvG.</summary>
    bool CanHitGvgTarget(Entity src, Entity target);

    /// <summary>True if the mob has an AI exception (rAthena MD_NORANDOMWALK etc.).</summary>
    bool HasAiException(MobEntity mob);
}
