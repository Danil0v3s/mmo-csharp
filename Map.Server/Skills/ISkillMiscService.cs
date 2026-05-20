using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// One-off special-skill commands grouped together so callers don't
/// have to invent a service per skill. Each method is a canonical
/// entry point for a single rAthena helper — most return false / 0
/// today because their data lives in the SC table or item DB; the
/// entry point is here so the skill cast lifecycle has a named
/// target.
///
/// rAthena reference points (skill.cpp):
/// <c>skill_sit</c>, <c>skill_greed</c>, <c>skill_frostjoke_scream</c>,
/// <c>skill_magicdecoy</c>, <c>skill_poisoningweapon</c>,
/// <c>skill_spellbook</c>, <c>skill_select_menu</c>,
/// <c>skill_graffitiremover</c>, <c>skill_detonator</c>,
/// <c>skill_maelstrom_suction</c>, <c>skill_check_camouflage</c>,
/// <c>skill_check_cloaking</c>, <c>skill_check_shadowform</c>,
/// <c>skill_dance_overlap</c>, <c>skill_toggle_magicpower</c>,
/// <c>skill_reveal_trap_inarea</c>, <c>skill_mirage_cast</c>,
/// <c>skill_isammotype</c>, <c>skill_check_bl_sc</c>,
/// <c>skill_shimiru_check_cell</c>.
/// </summary>
public interface ISkillMiscService
{
    /// <summary>rAthena <c>skill_sit</c> — Tension Relax sit-bonus.</summary>
    void Sit(PlayerEntity caster, bool sitting);

    /// <summary>rAthena <c>skill_greed</c> — pull every floor item within range to the caster.</summary>
    int Greed(PlayerEntity caster, short range);

    /// <summary>rAthena <c>skill_frostjoke_scream</c> — proc Frost Joke / Scream on nearby PCs.</summary>
    int FrostjokeScream(PlayerEntity caster);

    /// <summary>rAthena <c>skill_magicdecoy</c> — Warlock Magic Decoy place.</summary>
    bool MagicDecoy(PlayerEntity caster, ushort skillId, short x, short y);

    /// <summary>rAthena <c>skill_poisoningweapon</c> — Guillotine Cross Poisoning Weapon.</summary>
    bool PoisoningWeapon(PlayerEntity caster, int inventoryIndex);

    /// <summary>rAthena <c>skill_spellbook</c> — Sage Reading Spell Book consume.</summary>
    bool SpellBook(PlayerEntity caster, int inventoryIndex);

    /// <summary>rAthena <c>skill_select_menu</c> — Arrullo / Service for You menu choice.</summary>
    void SelectMenu(PlayerEntity caster, ushort skillId);

    /// <summary>rAthena <c>skill_graffitiremover</c> — remove a graffiti unit.</summary>
    bool GraffitiRemover(PlayerEntity caster, short x, short y);

    /// <summary>rAthena <c>skill_detonator</c> — proc all caster traps in range.</summary>
    int Detonator(PlayerEntity caster, short range);

    /// <summary>rAthena <c>skill_maelstrom_suction</c> — Maelstrom skill-absorb.</summary>
    bool MaelstromSuction(PlayerEntity caster, ushort suctioned);

    /// <summary>rAthena <c>skill_check_camouflage</c>.</summary>
    bool CheckCamouflage(Entity bl);

    /// <summary>rAthena <c>skill_check_cloaking</c>.</summary>
    bool CheckCloaking(Entity bl);

    /// <summary>rAthena <c>skill_check_shadowform</c>.</summary>
    bool CheckShadowForm(Entity bl);

    /// <summary>rAthena <c>skill_dance_overlap</c> — Dancer / Bard overlap rule.</summary>
    bool DanceOverlap(SkillUnitGroup group, bool add);

    /// <summary>rAthena <c>skill_toggle_magicpower</c> — Sage Magic Power toggle.</summary>
    void ToggleMagicPower(PlayerEntity caster, ushort skillId);

    /// <summary>rAthena <c>skill_reveal_trap_inarea</c> — Trap Reveal.</summary>
    int RevealTrapInArea(Entity caster, short range);

    /// <summary>rAthena <c>skill_mirage_cast</c> — Mirage Visor proc.</summary>
    bool MirageCast(Entity bl, ushort skillId);

    /// <summary>rAthena <c>skill_isammotype</c> — true if the skill uses ammo of the given type.</summary>
    bool IsAmmoType(PlayerEntity caster, ushort skillId);

    /// <summary>rAthena <c>skill_check_bl_sc</c> — predicate for SC-removal AoE.</summary>
    bool CheckBlSc(Entity bl, int statusType);

    /// <summary>rAthena <c>skill_shimiru_check_cell</c> — Shimiru cell-check.</summary>
    bool ShimiruCheckCell(short x, short y);
}
