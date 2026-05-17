using System.Diagnostics;
using System.Net.Sockets;
using MySqlConnector;

namespace Map.Server.Tests.Replay;

/// <summary>
/// xUnit class fixture that boots Login → Char → Map as child processes
/// (from their pre-built binaries, no rebuild) and tears them down at the
/// end of the test class. Each process gets its own log file under
/// <c>bin/.../Replay/Logs/</c> so a failed replay can be diagnosed by
/// reading the corresponding server log.
///
/// The fixture polls each TCP listener until it accepts a connection,
/// then moves on to the next server. Total boot time is on the order of
/// 10–15 s on a cold cache; subsequent boots in the same test run reuse
/// the binaries.
/// </summary>
public sealed class ServerStackFixture : IAsyncLifetime
{
    private readonly List<Process> _processes = new();
    private string _logDir = string.Empty;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        _logDir = Path.Combine(Path.GetTempPath(), $"mmo-replay-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_logDir);

        // The captured fixture exercises rAthena's "create account on login"
        // flow (CA_LOGIN with the _M / _F suffix). For that path to run, the
        // target account must not already exist in `login`. Wipe replay-test
        // accounts (and any characters they own) so consecutive runs start
        // from the same fresh state the capture was recorded against.
        await CleanReplayAccountsAsync();

        await StartAsync(repoRoot, "Login.Server", port: 6900);
        await StartAsync(repoRoot, "Char.Server", port: 6121);
        // Note: capture says 5121 but our config says 5191.
        await StartAsync(repoRoot, "Map.Server", port: 5191);

        // Each server exposes a CZ_INTERNAL_PING handler that responds with
        // its own IServerReadiness state. We block here until every server
        // reports Ready=1, which is stronger than "TCP port accepts":
        //   - Login: state == Running.
        //   - Char:  + registered with Login server.
        //   - Map:   + map list registered with Char server.
        // Without this, the replay races against char→login registration
        // and (more painfully) the map server's mob_db / item_db / world
        // cache load, which all run after the TCP listener binds.
        await WaitForServerReadyAsync(port: 6900, name: "Login", timeout: TimeSpan.FromSeconds(30));
        await WaitForServerReadyAsync(port: 6121, name: "Char",  timeout: TimeSpan.FromSeconds(30));
        await WaitForServerReadyAsync(port: 5191, name: "Map",   timeout: TimeSpan.FromSeconds(120));
    }

    private const string DbConnectionString =
        "Server=localhost;Port=3306;Database=ragnarok;Uid=ragnarok;Pwd=ragnarok;CharSet=utf8mb4;";

    /// <summary>
    /// Names that may appear as `userid` in the replay captures. Anything
    /// matching these is removed so the fresh-account flow runs as captured.
    /// </summary>
    private static readonly string[] ReplayAccountNames = { "mmocsharp", "danilo3" };

    private static async Task CleanReplayAccountsAsync()
    {
        await using var conn = new MySqlConnection(DbConnectionString);
        await conn.OpenAsync();

        // Clear login security state first: the replay capture's first
        // login attempt ("danilo3") is *expected* to fail with refuse=0
        // (unknown account). Consecutive test runs accumulate failed
        // logins in `loginlog`; after `DynamicPassFailureBanLimit` hits,
        // the next failure refuses with code 3 (banned) instead of 0,
        // which makes line 2 of the capture diverge. Clearing both
        // tables every run keeps the assertion stable.
        foreach (var sql in new[]
        {
            "DELETE FROM loginlog WHERE user = 'danilo3'",
            "DELETE FROM ipbanlist WHERE list IN ('127.*.*.*', '127.0.*.*', '127.0.0.*', '127.0.0.1')",
        })
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        // Resolve account ids first so we can fan out to dependent tables
        // without relying on FKs (the char schema deliberately avoids most).
        var ids = new List<int>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT account_id FROM login WHERE userid IN ("
                              + string.Join(",", ReplayAccountNames.Select((_, i) => $"@n{i}")) + ")";
            for (var i = 0; i < ReplayAccountNames.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@n{i}", ReplayAccountNames[i]);
            }
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) ids.Add(reader.GetInt32(0));
        }

        if (ids.Count == 0) return;

        var idList = string.Join(",", ids);
        // Delete in dependency order: characters owned by these accounts,
        // then the accounts themselves. Anything that fans out from `char`
        // (inventory, skills, etc.) is left to ON DELETE cascades where
        // configured; otherwise it's orphan rows that don't affect the
        // login → connect flow under test.
        foreach (var sql in new[]
        {
            $"DELETE FROM `char` WHERE account_id IN ({idList})",
            $"DELETE FROM login WHERE account_id IN ({idList})"
        })
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // CZ_INTERNAL_PING / ZC_INTERNAL_PONG wire format (see PacketHeader.cs):
    //   ping  = [0x30 0x75]                  (header only)
    //   pong  = [0x31 0x75 <Ready:1>]        (header + 1-byte ready flag)
    // Custom IDs in the 0x75xx range that rAthena never uses for clients.
    private const ushort CZ_INTERNAL_PING = 0x7530;
    private const ushort ZC_INTERNAL_PONG = 0x7531;
    private static readonly byte[] PingBytes = { (byte)(CZ_INTERNAL_PING & 0xFF), (byte)(CZ_INTERNAL_PING >> 8) };

    private static async Task WaitForServerReadyAsync(int port, string name, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new TcpClient();
                using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await client.ConnectAsync("127.0.0.1", port, connectCts.Token);
                using var stream = client.GetStream();
                stream.ReadTimeout = 2000;

                await stream.WriteAsync(PingBytes);
                await stream.FlushAsync();

                var response = new byte[3];
                var read = 0;
                while (read < response.Length)
                {
                    var n = await stream.ReadAsync(response.AsMemory(read, response.Length - read));
                    if (n == 0) break;
                    read += n;
                }

                if (read == 3
                    && BitConverter.ToUInt16(response, 0) == ZC_INTERNAL_PONG
                    && response[2] == 1)
                {
                    return;
                }
            }
            catch (Exception ex) { lastError = ex; }

            await Task.Delay(500);
        }
        throw new TimeoutException(
            $"{name} server (port {port}) did not report ready within {timeout.TotalSeconds}s"
            + (lastError != null ? $". Last error: {lastError.GetType().Name}: {lastError.Message}" : "."));
    }

    public Task DisposeAsync()
    {
        foreach (var p in _processes)
        {
            try
            {
                if (!p.HasExited) p.Kill(entireProcessTree: true);
            }
            catch { /* best-effort */ }
        }
        return Task.CompletedTask;
    }

    /// <summary>Directory holding `login.log`, `char.log`, `map.log` for this run.</summary>
    public string LogDirectory => _logDir;

    private async Task StartAsync(string repoRoot, string projectName, int port)
    {
        var logPath = Path.Combine(_logDir, $"{projectName.ToLowerInvariant().Replace(".server", "")}.log");
        var logStream = new FileStream(logPath, FileMode.CreateNew);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.Combine(repoRoot, projectName),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(Path.Combine(repoRoot, projectName));
        psi.ArgumentList.Add("--no-build");

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start {projectName}");
        _processes.Add(process);

        // Tee stdout+stderr into the log file. Don't block on the process.
        _ = Task.Run(async () =>
        {
            try
            {
                var stdout = process.StandardOutput.BaseStream.CopyToAsync(logStream);
                var stderr = process.StandardError.BaseStream.CopyToAsync(logStream);
                await Task.WhenAll(stdout, stderr);
            }
            catch { /* writer disposed on teardown */ }
        });

        await WaitForTcpAsync(port, TimeSpan.FromSeconds(30))
            .ConfigureAwait(false);
    }

    private static async Task WaitForTcpAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await client.ConnectAsync("127.0.0.1", port, cts.Token);
                return;
            }
            catch (SocketException) { }
            catch (OperationCanceledException) { }
            await Task.Delay(250);
        }
        throw new TimeoutException($"Port {port} did not accept connections within {timeout.TotalSeconds}s");
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test assembly's working dir looking for the .sln.
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "MMO.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        if (dir == null) throw new InvalidOperationException("Could not locate MMO.sln walking up from test bin/");
        return dir;
    }
}
