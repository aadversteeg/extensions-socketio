using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntegrationTests.Extensions.SocketIO.Client;

/// <summary>
/// xUnit fixture that manages a Node.js Socket.IO v2 test server process.
/// </summary>
public class SocketIOV2ServerFixture : IAsyncLifetime
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly string NpmCommand = IsWindows ? "npm.cmd" : "npm";

    private Process? _serverProcess;
    private readonly TimeSpan _startupTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the base URI of the running test server.
    /// </summary>
    public Uri ServerUri { get; private set; } = null!;

    /// <summary>
    /// Gets the port the server is listening on.
    /// </summary>
    public int Port { get; private set; }

    /// <summary>
    /// Gets whether Node.js is available on the system.
    /// </summary>
    public bool IsNodeAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        IsNodeAvailable = CheckNodeAvailable();
        if (!IsNodeAvailable)
        {
            return;
        }

        Port = GetFreePort();
        ServerUri = new Uri($"http://localhost:{Port}");

        var serverDir = GetServerDirectory();
        await RunNpmInstall(serverDir);
        StartServer(serverDir);
        await WaitForServerReady();
    }

    public Task DisposeAsync()
    {
        if (_serverProcess is not null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(5000);
            }
            catch
            {
                // Best effort cleanup
            }
            finally
            {
                _serverProcess.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    private static bool CheckNodeAvailable()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string GetServerDirectory()
    {
        // Navigate from the output directory up to the repository root, then into test/server-v2.
        // Output dir is typically: src/<ProjectName>/bin/Debug/net10.0/
        // Repository root is 5 levels up.
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var dir = baseDir;

        for (var i = 0; i < 5; i++)
        {
            var parent = Path.GetDirectoryName(dir);
            if (parent is null)
            {
                break;
            }
            dir = parent;
        }

        var serverDir = Path.Combine(dir, "test", "server-v2");

        if (Directory.Exists(serverDir))
        {
            return serverDir;
        }

        // Fallback: try the output directory copy
        var outputServerDir = Path.Combine(baseDir, "server-v2");
        if (Directory.Exists(outputServerDir))
        {
            return outputServerDir;
        }

        throw new DirectoryNotFoundException(
            $"Server directory not found. Searched '{serverDir}' and '{outputServerDir}'.");
    }

    private static async Task RunNpmInstall(string serverDir)
    {
        var nodeModulesDir = Path.Combine(serverDir, "node_modules");
        if (Directory.Exists(nodeModulesDir))
        {
            return;
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = NpmCommand,
            Arguments = "install --no-audit --no-fund",
            WorkingDirectory = serverDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();

        // Read streams to prevent deadlocks
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var stderr = await stderrTask;
            throw new InvalidOperationException($"npm install failed with exit code {process.ExitCode}: {stderr}");
        }
    }

    private void StartServer(string serverDir)
    {
        _serverProcess = new Process();
        _serverProcess.StartInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"server.js {Port}",
            WorkingDirectory = serverDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _serverProcess.Start();

        // Begin reading output asynchronously to prevent buffer deadlocks
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();
    }

    private async Task WaitForServerReady()
    {
        using var httpClient = new HttpClient();
        var pollUrl = $"http://localhost:{Port}/socket.io/?EIO=3&transport=polling";
        var deadline = DateTime.UtcNow + _startupTimeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync(pollUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet
            }

            if (_serverProcess is not null && _serverProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Server process exited unexpectedly with code {_serverProcess.ExitCode}");
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Server did not become ready within {_startupTimeout.TotalSeconds} seconds");
    }
}
