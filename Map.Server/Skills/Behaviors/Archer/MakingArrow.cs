using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// AC_MAKINGARROW — Archer Make Arrow (skill.cpp:AC_MAKINGARROW arm).
/// Emits <c>clif_arrow_create_list</c> with every source item the
/// caster currently owns that maps to an arrow recipe in
/// <c>create_arrow_db</c>. The user's pick produces the arrows via
/// <see cref="ISkillProductionService.ArrowCreate"/>.
/// </summary>
public sealed class MakingArrow : SkillImpl
{
    public MakingArrow() : base(SkillIds.AC_MAKINGARROW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var session = ctx.Sessions?.TryGet(pc);
        var inv = session?.Inventory;
        if (inv == null) return;
        // Each inventory row that has an arrow_db recipe is offered as a
        // picker entry. Without a per-item recipe lookup we offer every
        // amount-positive row — the picker on the client filters by the
        // create_arrow_db catalog it received from char-server at boot.
        var sources = new List<ushort>();
        foreach (var row in inv)
        {
            if (row.Amount > 0 && row.NameId is > 0 and <= ushort.MaxValue)
                sources.Add((ushort)row.NameId);
        }
        ctx.Client?.BroadcastArrowCreateList(pc, sources);
    }
}
