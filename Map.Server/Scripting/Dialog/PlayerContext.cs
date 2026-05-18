using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

/// <summary>
/// The player surface exposed to TS authors as <c>ctx.player</c>. Most
/// fields are computed projections of <see cref="MapSessionData.CharacterData"/>
/// (the loaded snapshot from char-server) and <see cref="PlayerEntity"/>
/// (in-memory current values). Mutating properties write back to the same
/// source and broadcast a <c>ZC_PAR_CHANGE</c> so the client UI reflects
/// the change immediately.
///
/// Lowercase names match the JS contract; ClearScript's host-object binding
/// is case-sensitive when mapping <c>ctx.player.foo</c> to a CLR member.
///
/// Persistence note: changes to fields that live on
/// <see cref="MapSessionData.CharacterData"/> (zeny, etc.) are pushed back
/// to the char-server on autosave / logout via SaveCharacterState IPC. The
/// IPC currently doesn't carry the full mutated state payload — that's a
/// separate slice. For now, changes are visible to the client this session
/// but reset on logout. Authors should treat persistence as "best effort"
/// until that slice lands.
/// </summary>
public sealed class PlayerContext
{
    private readonly MapSessionData _session;
    private readonly PlayerEntity _entity;

    /// <summary>
    /// Memory-only variable scope (rAthena's <c>@var</c>). Set on the
    /// session at first access and persists for the player's connected
    /// session. JS authors write <c>ctx.player.session.foo = 1</c>.
    /// </summary>
    public PropertyBag session { get; }

    public PlayerContext(MapSessionData session, PlayerEntity entity)
    {
        _session = session;
        _entity = entity;
        session.ScriptSessionVars ??= new PropertyBag();
        this.session = session.ScriptSessionVars;
    }

    // ---- identity (read-only) ----
    public int id => _entity.CharacterId;
    public int accountId => _entity.AccountId;
    public string name => _entity.Name;
    public int sex => _session.Sex;
    public int classId => (int)(_session.CharacterData?.ClassId ?? 0);
    public int baseLevel => (int)(_session.CharacterData?.BaseLevel ?? 1);
    public int jobLevel => (int)(_session.CharacterData?.JobLevel ?? 1);

    // ---- stats (read-only for this slice) ----
    public int str => (int)(_session.CharacterData?.Str ?? 1);
    public int agi => (int)(_session.CharacterData?.Agi ?? 1);
    public int vit => (int)(_session.CharacterData?.Vit ?? 1);
    [ScriptMember("int")] public int intStat => (int)(_session.CharacterData?.IntStat ?? 1);
    public int dex => (int)(_session.CharacterData?.Dex ?? 1);
    public int luk => (int)(_session.CharacterData?.Luk ?? 1);

    // ---- hp / sp (current values are on PlayerEntity; the broadcast on
    //      heal updates them and tells the client) ----
    public int hp => _entity.Hp;
    public int maxHp => _entity.MaxHp;
    public int sp => _entity.Sp;
    public int maxSp => _entity.MaxSp;

    // ---- zeny (lives on CharacterData; setter broadcasts SP_ZENY via
    //      ZC_LONGPAR_CHANGE — rAthena clif_updatestatus dispatches zeny
    //      to clif_longpar_change, NOT clif_par_change, because the long-
    //      par packet is the one modern clients are wired to honour for
    //      currency / exp updates) ----
    public int zeny
    {
        get => (int)(_session.CharacterData?.Zeny ?? 0);
        set
        {
            if (_session.CharacterData is null) return;
            var clamped = Math.Max(0, value);
            _session.CharacterData.Zeny = (uint)clamped;
            _session.EnqueuePacket(new ZC_LONGPAR_CHANGE
            {
                VarId = SpId.SP_ZENY,
                Value = clamped,
            });
        }
    }

    // ---- methods ----

    /// <summary>
    /// Restore HP and optionally SP. Both clamp to their max. Broadcasts
    /// <c>ZC_PAR_CHANGE</c> for each affected stat. Returns nothing because
    /// no client round-trip is needed; the script's <c>await</c> on this
    /// resolves immediately so it composes with other dialog steps.
    /// </summary>
    public Task heal(int hp, int sp = 0)
    {
        if (hp != 0)
        {
            _entity.Hp = Math.Clamp(_entity.Hp + hp, 0, _entity.MaxHp);
            Par(SpId.SP_HP, _entity.Hp);
        }
        if (sp != 0)
        {
            _entity.Sp = Math.Clamp(_entity.Sp + sp, 0, _entity.MaxSp);
            Par(SpId.SP_SP, _entity.Sp);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send a self-only system chat line to the player. Convenience for
    /// debugging / quick feedback that doesn't open a dialog window.
    /// </summary>
    public Task message(string text)
    {
        _session.EnqueuePacket(new ZC_NOTIFY_PLAYERCHAT { Message = text ?? string.Empty });
        return Task.CompletedTask;
    }

    private void Par(ushort spId, int value)
    {
        _session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = spId, Value = value });
    }
}
