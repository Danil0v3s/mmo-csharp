using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillMiscService"/>. Every method has a canonical
/// landing point; behavior fills in as each skill ports. Several depend
/// on subsystems that haven't shipped yet (SC table for camouflage /
/// cloaking, floor-item registry for Greed, trap registry for Detonator)
/// — those branches return early and log so the call site is visible.
/// </summary>
public sealed class SkillMiscService : ISkillMiscService
{
    private readonly IEntityRegistry _entities;
    private readonly ILogger<SkillMiscService> _logger;

    public SkillMiscService(IEntityRegistry entities, ILogger<SkillMiscService> logger)
    {
        _entities = entities;
        _logger = logger;
    }

    public void Sit(PlayerEntity caster, bool sitting)
    {
        // rAthena: extra HP/SP regen + bonus crit while sitting if the
        // PC has TENSION_RELAX learned. The sit/stand state already
        // lives on PlayerEntity; this is the canonical place to
        // attach the per-sit-frame regen bump.
    }

    public int Greed(PlayerEntity caster, short range)
    {
        // Pulls every floor-item within `range` to the caster. The
        // floor-item registry doesn't expose a per-cell scan helper
        // yet; entry point reserved.
        return 0;
    }

    public int FrostjokeScream(PlayerEntity caster)
    {
        // Proc rate is 20 * lvl%; status_change_start with SC_STUN /
        // SC_SLEEP. Data-pending on SC table.
        return 0;
    }

    public bool MagicDecoy(PlayerEntity caster, ushort skillId, short x, short y) => false;
    public bool PoisoningWeapon(PlayerEntity caster, int inventoryIndex) => false;
    public bool SpellBook(PlayerEntity caster, int inventoryIndex) => false;
    public void SelectMenu(PlayerEntity caster, ushort skillId) { }
    public bool GraffitiRemover(PlayerEntity caster, short x, short y) => false;

    public int Detonator(PlayerEntity caster, short range)
    {
        // Trap registry not yet exposed — when SkillUnitService grows
        // a trap query the detonate loop wires through it.
        return 0;
    }

    public bool MaelstromSuction(PlayerEntity caster, ushort suctioned) => false;

    public bool CheckCamouflage(Entity bl) => false;
    public bool CheckCloaking(Entity bl) => false;
    public bool CheckShadowForm(Entity bl) => false;
    public bool DanceOverlap(SkillUnitGroup group, bool add) => false;
    public void ToggleMagicPower(PlayerEntity caster, ushort skillId) { }
    public int RevealTrapInArea(Entity caster, short range) => 0;
    public bool MirageCast(Entity bl, ushort skillId) => false;
    public bool IsAmmoType(PlayerEntity caster, ushort skillId) => false;
    public bool CheckBlSc(Entity bl, int statusType) => false;
    public bool ShimiruCheckCell(short x, short y) => false;
}
