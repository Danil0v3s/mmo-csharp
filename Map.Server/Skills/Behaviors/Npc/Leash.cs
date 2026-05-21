using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_LEASH — Pull target adjacent to caster (slide), broadcast no-damage.</summary>
public sealed class Leash : SkillImpl
{
    private readonly IUnitOpsService? _units;
    public Leash() : base(SkillIds.NPC_LEASH) { }
    public Leash(IUnitOpsService? units = null) : base(SkillIds.NPC_LEASH) { _units = units; }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _units?.MovePos(target, src.X, src.Y, easy: 1, checkColl: true);
    }
}
