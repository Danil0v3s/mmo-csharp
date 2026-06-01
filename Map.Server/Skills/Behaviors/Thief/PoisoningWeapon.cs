using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_POISONINGWEAPON — Poisoning Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/thief/poisoningweapon.cpp</c>.
/// rAthena opens the poison-list dialog (<c>clif_poison_list</c>) so
/// the player picks which Guillotine Cross poison to coat. The
/// dialog wiring is client-driven; the server-side handshake landing
/// the choice is what then starts SC_POISONINGWEAPON on the caster.
///
/// <para>🚩 INFRA-DEFERRED — <c>clif_poison_list</c> requires a
/// dedicated packet sender + the matching follow-up packet handler
/// that consumes the player's choice and routes it back through
/// SC_POISONINGWEAPON's Val1 / Val2. Both are out of scope for the
/// per-skill behavior file. The broadcast frame below keeps the
/// animation on parity.</para>
/// </summary>
public sealed class PoisoningWeapon : SkillImpl
{
    public PoisoningWeapon() : base(SkillIds.GC_POISONINGWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
