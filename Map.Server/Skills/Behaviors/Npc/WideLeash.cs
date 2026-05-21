using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDELEASH — Splash leash; pull each enemy in splash to src. Splash iteration TODO.</summary>
public sealed class WideLeash : SkillImpl
{
    private readonly IUnitOpsService? _units;
    public WideLeash() : base(SkillIds.NPC_WIDELEASH) { }
    public WideLeash(IUnitOpsService? units = null) : base(SkillIds.NPC_WIDELEASH) { _units = units; }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _units?.MovePos(target, src.X, src.Y, easy: 1, checkColl: true);
    }
}
