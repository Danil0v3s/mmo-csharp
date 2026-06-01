namespace Map.Server.Entities;

/// <summary>
/// COMBAT-10 — a PC's persisted <b>base</b> allocated primary + trait stats,
/// the C# analogue of rAthena <c>sd-&gt;status.str</c> .. <c>status.crt</c>
/// (the values written by <c>pc_statusup</c> / <c>pc_traitstatusup</c> and
/// loaded from <c>mmo_charstatus</c>).
///
/// <para>This is deliberately distinct from <see cref="Map.Server.Status.BattleStats"/>'s
/// <c>Str</c>..<c>Crt</c>, which hold the <b>final</b> battle stat
/// (<c>base + card param_bonus + equip param_equip + job_bonus + SC</c>,
/// status.cpp:4244-4266). Every recalc-input builder reads <b>this</b> as the
/// base so <see cref="Map.Server.Status.IStatusCalcService.CalcPc"/> can
/// re-layer equipment + job bonus idempotently without the final value being
/// read back into itself (which would double-count on the next recalc — the
/// exact bug COMBAT-10 fixes).</para>
///
/// Index order mirrors rAthena <c>PARAM_*</c>:
/// 0=Str 1=Agi 2=Vit 3=Int 4=Dex 5=Luk 6=Pow 7=Sta 8=Wis 9=Spl 10=Con 11=Crt.
/// </summary>
public sealed class PcBaseParams
{
    public const int Count = 12;

    public short Str;
    public short Agi;
    public short Vit;
    public short IntStat;
    public short Dex;
    public short Luk;
    public short Pow;
    public short Sta;
    public short Wis;
    public short Spl;
    public short Con;
    public short Crt;

    public short this[int i]
    {
        get => i switch
        {
            0 => Str, 1 => Agi, 2 => Vit, 3 => IntStat, 4 => Dex, 5 => Luk,
            6 => Pow, 7 => Sta, 8 => Wis, 9 => Spl, 10 => Con, 11 => Crt,
            _ => 0,
        };
        set
        {
            switch (i)
            {
                case 0: Str = value; break;
                case 1: Agi = value; break;
                case 2: Vit = value; break;
                case 3: IntStat = value; break;
                case 4: Dex = value; break;
                case 5: Luk = value; break;
                case 6: Pow = value; break;
                case 7: Sta = value; break;
                case 8: Wis = value; break;
                case 9: Spl = value; break;
                case 10: Con = value; break;
                case 11: Crt = value; break;
            }
        }
    }
}
