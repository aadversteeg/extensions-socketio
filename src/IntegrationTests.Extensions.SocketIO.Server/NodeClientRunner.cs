using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class NodeClientRunner : IDisposable
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static readonly string NpmCommand = IsWindows ? "npm.cmd" : "npm";

    private static bool _npmInstallDone;
    private static readonly object NpmInstallLock = new();

    private Process? _process;

    public bool IsNodeAvailable { get; }

    public NodeClientRunner()
    {
        IsNodeAvailable = CheckNodeAvailable();
    }

    public async Task<IReadOnlyList<NodeClientMessage>> RunScenarioAsync(
        int port,
        string scenario,
        object? args = null,
        TimeSpan? timeout = null)
    {
        var session = StartScenario(port, scenario, args);
        return await session.WaitForCompletionAsync(timeout ?? TimeSpan.FromSeconds(15)).ConfigureAwait(false);
    }

    public ClientSession StartScenario(
        int port,
        string scenario,
        object? args = null)
    {
        if (!IsNodeAvailable)
        {
            return new ClientSession(null, new ConcurrentBag<NodeClientMessage>(), new TaskCompletionSource<bool>());
        }

        var clientDir = GetClientDirectory();
        EnsureNpmInstall(clientDir);

        var argsJson = args != null
            ? JsonSerializer.Serialize(args, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            : null;

        var arguments = $"client.js {port} {scenario}";
        if (argsJson != null)
        {
            arguments += $" {EscapeArgument(argsJson)}";
        }

        _process = new Process();
        _process.StartInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = arguments,
            WorkingDirectory = clientDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var messages = new ConcurrentBag<NodeClientMessage>();
        var outputComplete = new TaskCompletionSource<bool>();

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                outputComplete.TrySetResult(true);
                return;
            }

            try
            {
                var msg = JsonSerializer.Deserialize<NodeClientMessage>(e.Data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
                if (msg != null)
                {
                    messages.Add(msg);
                }
            }
            catch
            {
                // Ignore non-JSON output
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        return new ClientSession(_process, messages, outputComplete);
    }

    public void Dispose()
    {
        if (_process is not null && !_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
            catch
            {
                // Best effort cleanup
            }
            finally
            {
                _process.Dispose();
            }
        }
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

    private static string GetClientDirectory()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var dir = baseDir;

        for (var i = 0; i < 5; i++)
        {
            var parent = Path.GetDirectoryName(dir);
            if (parent is null) break;
            dir = parent;
        }

        var clientDir = Path.Combine(dir, "test", "server-test-client");
        if (Directory.Exists(clientDir))
        {
            return clientDir;
        }

        var outputClientDir = Path.Combine(baseDir, "server-test-client");
        if (Directory.Exists(outputClientDir))
        {
            return outputClientDir;
        }

        throw new DirectoryNotFoundException(
            $"Client directory not found. Searched '{clientDir}' and '{outputClientDir}'.");
    }

    private static void EnsureNpmInstall(string clientDir)
    {
        if (_npmInstallDone) return;

        lock (NpmInstallLock)
        {
            if (_npmInstallDone) return;

            var nodeModulesDir = Path.Combine(clientDir, "node_modules");
            if (!Directory.Exists(nodeModulesDir))
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = NpmCommand,
                    Arguments = "install --no-audit --no-fund",
                    WorkingDirectory = clientDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                process.Start();
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"npm install failed with exit code {process.ExitCode} in '{clientDir}'. stderr: {stderr}. stdout: {stdout}");
                }
            }

            _npmInstallDone = true;
        }
    }

    private static string EscapeArgument(string arg)
    {
        if (IsWindows)
        {
            return "\"" + arg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
        return "'" + arg.Replace("'", "'\\''") + "'";
    }
}

public class ClientSession
{
    private readonly Process? _process;
    private readonly ConcurrentBag<NodeClientMessage> _messages;
    private readonly TaskCompletionSource<bool> _outputComplete;

    internal ClientSession(
        Process? process,
        ConcurrentBag<NodeClientMessage> messages,
        TaskCompletionSource<bool> outputComplete)
    {
        _process = process;
        _messages = messages;
        _outputComplete = outputComplete;
    }

    public IReadOnlyList<NodeClientMessage> Messages => _messages.ToArray();

    public async Task<IReadOnlyList<NodeClientMessage>> WaitForCompletionAsync(TimeSpan? timeout = null)
    {
        if (_process == null)
        {
            return Array.Empty<NodeClientMessage>();
        }

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);
        using var cts = new CancellationTokenSource(effectiveTimeout);

        try
        {
            await _process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { _process.Kill(entireProcessTree: true); } catch { }
        }

        // Wait for output to flush
        await Task.WhenAny(_outputComplete.Task, Task.Delay(2000)).ConfigureAwait(false);

        return _messages.ToArray();
    }
}

public class NodeClientMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    [JsonPropertyName("client")]
    public int? Client { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("eventCount")]
    public int? EventCount { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("clients")]
    public JsonElement? Clients { get; set; }
}
