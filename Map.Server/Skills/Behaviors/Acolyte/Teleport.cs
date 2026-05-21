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
            // Mob caster: random-warp on same map. TODO: hook into
            // IUnitOpsService.Warp once it lands.
            return;
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // Send the chooser packet at lv1 (just "Random") or lv2+ (Random + Save).
        var maps = new List<string> { "Random" };
        if (skillLevel >= 2) maps.Add("SavePoint"); // TODO: actual save_point.map.

        _visibility?.SendToSelf(sd, new ZC_WARPLIST
        {
            SkillId = SkillId,
            Maps = maps,
        });
    }
}
