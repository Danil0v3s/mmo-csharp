using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Visibility;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_BODYRELOCATION — Monk Snap / Body Relocation. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/snap.cpp</c>.
///
/// <para>Instant displacement to the target cell (max range from
/// skill_db, walkability check via <c>unit_movepos</c>). On success
/// emits <c>clif_snap</c> (renewal-PACKETVER = ZC_HIGHJUMP slide
/// visual) and locks the caster out of MO_EXTREMITYFIST for 2 s.</para>
/// </summary>
public sealed class Snap : SkillImpl
{
    private readonly IUnitOpsService? _unitOps;
    private readonly IVisibilityService? _visibility;
    private readonly Map.Server.Skills.ISkillBlockService? _block;

    public Snap() : base(SkillIds.MO_BODYRELOCATION) { }

    public Snap(
        IUnitOpsService? unitOps = null,
        IVisibilityService? visibility = null,
        Map.Server.Skills.ISkillBlockService? block = null)
        : base(SkillIds.MO_BODYRELOCATION)
    {
        _unitOps = unitOps;
        _visibility = visibility;
        _block = block;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (unit_movepos(src, x, y, 2, 1)) { clif_snap(...); skill_blockpc_start(EXTREMITYFIST, 2000); }
        var moved = _unitOps?.MovePos(src, x, y, easy: 2, checkColl: true) ?? false;
        if (!moved) return;

        // Snap visual — ZC_HIGHJUMP at the new position.
        _visibility?.SendToArea(src, new ZC_HIGHJUMP
        {
            SrcId = src.Id.Value,
            X = src.X,
            Y = src.Y,
        });

        // rAthena: skill_blockpc_start(sd, MO_EXTREMITYFIST, 2000) — lock
        // EXTREMITYFIST for 2 s following the relocation.
        if (src is PlayerEntity caster && _block != null)
        {
            _block.BlockPcStart(caster, SkillIds.MO_EXTREMITYFIST, 2000);
        }
    }
}
