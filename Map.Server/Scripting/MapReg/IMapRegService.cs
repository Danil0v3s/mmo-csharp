namespace Map.Server.Scripting.MapReg;

/// <summary>
/// Persistent map-server-wide script variables (`$variable`,
/// `$@variable`, `#variable`). Canonical entry points for rAthena
/// <c>mapreg.cpp</c> (355 lines, 10 functions).
///
/// rAthena stores these in the `mapreg` SQL table; the map server
/// reads at boot, writes on script `set $foo,...`. Player-scoped
/// registers (`@var`, `#var`) live on PlayerEntity already; this
/// service owns the *server-scoped* ones.
/// </summary>
public interface IMapRegService
{
    /// <summary>rAthena <c>mapreg_readreg</c> — read an int register.</summary>
    long ReadReg(int key);

    /// <summary>rAthena <c>mapreg_readregstr</c> — read a string register.</summary>
    string? ReadRegStr(int key);

    /// <summary>rAthena <c>mapreg_setreg</c> — write an int register.</summary>
    bool SetReg(int key, long value);

    /// <summary>rAthena <c>mapreg_setregstr</c> — write a string register.</summary>
    bool SetRegStr(int key, string? value);

    /// <summary>rAthena <c>mapreg_destroyreg</c> — delete a register.</summary>
    int DestroyReg(int key);

    /// <summary>rAthena <c>mapreg_init</c> — load from SQL.</summary>
    void Init();

    /// <summary>rAthena <c>mapreg_final</c> — flush to SQL.</summary>
    void Final();

    /// <summary>rAthena <c>mapreg_reload</c>.</summary>
    void Reload();

    /// <summary>rAthena <c>mapreg_config_read</c> — read configure file knobs.</summary>
    bool ConfigRead(string configPath);
}
