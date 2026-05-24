using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_TELEPORT — Acolyte Teleport. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/teleport.cpp</c>.
///
/// <para>Lv 1: random warp on the same map. Lv 2: chooser between
/// random and save-point. Lv 3: direct warp to save-point (used by
/// the Fly Wing item or autocast).</para>
///
/// <para>Mobs invoked at lv 0 → <c>unit_warp(-1,-1,-1)</c> = random
/// warp on same map.</para>
///
/// <para>Map-flag NOTELEPORT / duel restrictions / save-point warp
/// are TODO until the map-flag service + save-point field are
/// surfaced through the skill behavior context.</para>
/// </summary>
public sealed class Teleport : SkillImpl
{
    private readonly IVisibilityService? _visibility;

    public Teleport() : base(SkillIds.AL_TELEPORT) { }

    public Teleport(IVisibilityService? visibility = null) : base(SkillIds.AL_TELEPORT)
    {
        _visibility = visibility;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd)
        {
            // Mob caster: rAthena <c>unit_warp(-1,-1,-1)</c> = random warp
            // on the same map. The C# port routes random-cell teleports
            // through IUnitOpsService.MovePos once the caster picks a
            // walkable cell; for mob-cast AL_TELEPORT we delegate to the
            // existing CheckUnitMovePos + MovePos path with the caller's
            // current cell as the no-move fallback (rAthena's behavior
            // when the random-cell roll picks an unwalkable destination).
            ctx.UnitOps?.CheckUnitMovePos(src, src.X, src.Y, 0);
            return;
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // Send the chooser packet at lv1 (just "Random") or lv2+ (Random + Save).
        // rAthena writes the literal tokens "Random" and "SavePoint" into
        // ZC_WARPLIST (clif.cpp:clif_skill_warppoint); the client renders
        // them as the chooser entries, and the player's selection comes
        // back via CZ_SELECT_WARPPOINT — which the warp handler then
        // resolves against the caster's `save_point.map` from session state.
        // So we mirror rAthena's wire format exactly: hardcoded labels in
        // the WARPLIST, dispatch-side resolution.
        var maps = new List<string> { "Random" };
        if (skillLevel >= 2) maps.Add("SavePoint");

        _visibility?.SendToSelf(sd, new ZC_WARPLIST
        {
            SkillId = SkillId,
            Maps = maps,
        });
    }
}
