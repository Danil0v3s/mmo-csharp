using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_UNDEADATTACK — Element-undead weapon attack. Ratio +100*(lv-1); +20% hit rate.</summary>
public sealed class UndeadElementAttack : WeaponSkillImpl
{
    public UndeadElementAttack() : base(SkillIds.NPC_UNDEADATTACK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);
    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 20 / 100);
}
