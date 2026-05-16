using System.Diagnostics;
using System.Net.Sockets;

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

        await StartAsync(repoRoot, "Login.Server", port: 6900);
        await StartAsync(repoRoot, "Char.Server", port: 6121);
        // Note: capture says 5121 but our config says 5191.
        await StartAsync(repoRoot, "Map.Server", port: 5191);
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
