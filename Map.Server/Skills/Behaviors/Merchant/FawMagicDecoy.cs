using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGICDECOY — Mechanic FAW Magic Decoy. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/fawmagicdecoy.cpp</c>.
/// Opens the magic-decoy picker. UI packet TODO.
/// </summary>
public sealed class FawMagicDecoy : SkillImpl
{
    public FawMagicDecoy() : base(SkillIds.NC_MAGICDECOY) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: clif_magicdecoy_list emits the elemental-decoy picker — the
        // packet + the four BL_MOB Magic Decoy mob ids aren't ported yet.
    }
}
