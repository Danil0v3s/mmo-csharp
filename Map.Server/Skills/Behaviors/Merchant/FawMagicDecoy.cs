using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_MAGICDECOY — Mechanic FAW Magic Decoy (skill.cpp:NC_MAGICDECOY
/// arm). Opens the magic-decoy picker via <c>clif_magicdecoy_list</c>.
/// Caster picks one of four elemental Magic Decoy mobs (Fire / Water
/// / Wind / Earth); the pick spawns a BL_MOB at (x, y). Mob spawn
/// rides on the existing IMobOnceSpawnService — surfaced via ctx.MobSpawn.
/// </summary>
public sealed class FawMagicDecoy : SkillImpl
{
    // rAthena mob_db ids for the 4 Magic Decoy variants.
    private const ushort DecoyFire = 2043;
    private const ushort DecoyWater = 2044;
    private const ushort DecoyWind = 2045;
    private const ushort DecoyEarth = 2046;

    public FawMagicDecoy() : base(SkillIds.NC_MAGICDECOY) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        ushort[] decoys = { DecoyFire, DecoyWater, DecoyWind, DecoyEarth };
        ctx.Client?.BroadcastMagicDecoyList(pc, decoys);
    }
}
