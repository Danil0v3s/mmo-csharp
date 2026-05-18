using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

/// <summary>
/// The player surface exposed to TS authors as <c>ctx.player</c>. Reads
/// project from <see cref="MapSessionData.CharacterData"/> + <see cref="PlayerEntity"/>.
///
/// Writes use a *dirty-track + batch-flush* pattern: each property setter
/// mutates the backing field AND records the new value in
/// <see cref="_dirty"/>. The dispatcher calls <see cref="Flush"/> on every
/// suspending dialog step (<c>mes</c> / <c>next</c> / <c>select</c> /
/// <c>close</c>) so client packets ship in deterministic order with
/// dialog steps. Two consecutive writes to the same stat coalesce into
/// one outgoing packet.
///
/// Most of the rAthena script API surface lives here as <em>stubs</em>:
/// methods that log via <see cref="ScriptStub"/> and return placeholders.
/// Stubs let scripts call the full API without crashing while the
/// internals land in follow-up commits.
/// </summary>
public sealed partial class PlayerContext
{
    private const string Cat = "player";

    private readonly MapSessionData _session;
    private readonly PlayerEntity _entity;

    /// <summary>Last-known value per dirty stat. SpId → value.</summary>
    private readonly Dictionary<ushort, int> _dirty = new();

    /// <summary>
    /// Stats that ship over <see cref="ZC_LONGPAR_CHANGE"/> (0x00b1) rather
    /// than <see cref="ZC_PAR_CHANGE"/> (0x00b0). rAthena's
    /// <c>clif_updatestatus</c> dispatch table is the source of truth;
    /// currency (and pre-renewal exp before 20170830) goes through long-par.
    /// </summary>
    private static readonly HashSet<ushort> LongParStats = new()
    {
        SpId.SP_ZENY,
    };

    /// <summary>
    /// Memory-only variable scope (rAthena's <c>@var</c>). Set on the
    /// session at first access; lifetime tied to the player's connected
    /// session, reset on disconnect. JS authors write
    /// <c>ctx.player.session.foo = 1</c>.
    /// </summary>
    public PropertyBag session { get; }

    /// <summary>rAthena bare <c>var</c> — per-character permanent (DB-backed).</summary>
    public PropertyBag perm { get; }

    /// <summary>rAthena <c>#var</c> — per-account local (DB-backed).</summary>
    public PropertyBag account { get; }

    /// <summary>rAthena <c>##var</c> — per-account global (DB-backed).</summary>
    public PropertyBag accountGlobal { get; }

    /// <summary>Quest log surface (set/complete/erase/check/info).</summary>
    public PlayerQuestSurface quest { get; }
    /// <summary>Achievement surface (add/remove/complete/exists/update/info).</summary>
    public PlayerAchievementSurface achievement { get; }
    /// <summary>Personal storage (open/items).</summary>
    public PlayerStorageSurface storage { get; }
    /// <summary>Cart inventory (open/items/check).</summary>
    public PlayerCartSurface cart { get; }
    /// <summary>Mail surface (open).</summary>
    public PlayerMailSurface mail { get; }
    /// <summary>Pet surface (info/birth/skill/recovery/etc.).</summary>
    public PlayerPetSurface pet { get; }
    /// <summary>Homunculus surface (info/evolve/morph/mutate).</summary>
    public PlayerHomSurface hom { get; }
    /// <summary>Mercenary surface (create/delete/heal/info).</summary>
    public PlayerMercSurface merc { get; }

    public PlayerContext(MapSessionData session, PlayerEntity entity)
    {
        _session = session;
        _entity = entity;
        session.ScriptSessionVars ??= new PropertyBag();
        this.session = session.ScriptSessionVars;

        // The three persistent scopes were loaded in the connect flow and
        // stored on the session. If the load didn't run (early dev / test
        // setups), expose empty bags so script reads / writes don't NPE;
        // the saver no-ops when VarRegs is null.
        var regs = session.VarRegs ?? Map.Server.Persistence.PlayerVarRegs.Empty();
        perm = regs.Perm.Bag;
        account = regs.Account.Bag;
        accountGlobal = regs.AccountGlobal.Bag;

        quest = new PlayerQuestSurface();
        achievement = new PlayerAchievementSurface();
        storage = new PlayerStorageSurface();
        cart = new PlayerCartSurface();
        mail = new PlayerMailSurface();
        pet = new PlayerPetSurface();
        hom = new PlayerHomSurface();
        merc = new PlayerMercSurface();
    }
}
