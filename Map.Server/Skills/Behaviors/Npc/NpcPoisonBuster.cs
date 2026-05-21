using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_POISON_BUSTER — Magic hit consuming SC_POISON for bonus damage.</summary>
public sealed class NpcPoisonBuster : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    public NpcPoisonBuster() : base(SkillIds.NPC_POISON_BUSTER) { }
    public NpcPoisonBuster(ISkillAttackService? skillAttack = null) : base(SkillIds.NPC_POISON_BUSTER) { _skillAttack = skillAttack; }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 1000 * skillLevel;
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Poison) != null)
        {
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
            ctx.Sc.End(target, StatusType.Poison);
            return;
        }
        if (src is PlayerEntity sd)
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
    }
}
