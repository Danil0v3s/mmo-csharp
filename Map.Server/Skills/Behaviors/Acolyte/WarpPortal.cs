using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_WARP — Acolyte Warp Portal (chooser phase). Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/warpportal.cpp</c>.
///
/// <para>This is the <b>cast-phase</b> resolution: the player has
/// just finished casting AL_WARP on a ground cell. The server's
/// job here is to send the destination chooser
/// (<see cref="ZC_WARPLIST"/>) so the client can render the
/// "Choose destination" dialog. The actual warp happens later when
/// the client replies with <c>CZ_SELECT_WARPPOINT</c> (not in this
/// file — see the warp handler).</para>
///
/// <para>Available destinations are level-gated per rAthena:</para>
/// <list type="bullet">
///   <item>Lv 1: save_point only</item>
///   <item>Lv 2: save_point + memo_point[0]</item>
///   <item>Lv 3: save_point + memo_point[0..1]</item>
///   <item>Lv 4+: save_point + memo_point[0..2]</item>
/// </list>
///
/// <para>SC_CURSEDCIRCLE_ATKER is consumed at cast resolution so the
/// caster can act again (mirrors the explicit comment in the fork:
/// "Should only remove after the skill has been casted.").</para>
/// </summary>
public sealed class WarpPortal : SkillImpl
{
    private readonly IVisibilityService? _visibility;

    public WarpPortal() : base(SkillIds.AL_WARP) { }

    public WarpPortal(IVisibilityService? visibility = null) : base(SkillIds.AL_WARP)
    {
        _visibility = visibility;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: map_session_data* sd = BL_CAST(BL_PC, src);
        // Only players can cast AL_WARP — NPCs / mobs would never reach
        // this entry point because the cast gate filters non-PC sources.
        if (src is not PlayerEntity sd) return;

        // rAthena: std::vector<std::string> maps; maps.push_back(save_point.map);
        // Build the destination list level-gated by skill level.
        var maps = new List<string>(4);

        // First entry is always the save_point. The C# PlayerEntity
        // doesn't yet expose a save_point field (TODO: hydrate from
        // char_db save_map column) — fall back to a sentinel the client
        // recognizes as "Save Point" until that field lands.
        maps.Add("SavePoint");

        for (var i = 0; i < 3 && skillLevel >= (ushort)(i + 2); i++)
        {
            var memo = sd.MemoPoints[i];
            if (!string.IsNullOrEmpty(memo.MapName))
            {
                maps.Add(memo.MapName);
            }
        }

        // rAthena: clif_skill_warppoint(*sd, getSkillId(), skill_lv, maps);
        if (_visibility != null)
        {
            _visibility.SendToSelf(sd, new ZC_WARPLIST
            {
                SkillId = SkillId,
                Maps = maps,
            });
        }

        // rAthena: if (sc && sc->getSCE(SC_CURSEDCIRCLE_ATKER))
        //          status_change_end(src, SC_CURSEDCIRCLE_ATKER);
        // Comment in the fork: "Should only remove after the skill has
        // been casted." — i.e. consume the SC at completion, not on
        // cast start, so the caster is locked through the cast.
        ctx.Sc?.End(src, StatusType.CursedcircleAtker);

        // rAthena: flag |= SKILL_NOCONSUME_REQ;
        // The cast doesn't consume the Blue Gemstone requirement here;
        // the gem is consumed when the player picks a destination and
        // the actual warp portal SkillUnit is created (handled by
        // CZ_SELECT_WARPPOINT). Our IRequirementService handles the
        // gem consumption at unit-placement time, so the flag is
        // implicit (we don't run consumption on this cast path).
    }
}
