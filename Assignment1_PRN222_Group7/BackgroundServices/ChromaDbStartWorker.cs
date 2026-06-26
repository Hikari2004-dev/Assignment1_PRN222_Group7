using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.BackgroundServices
{
    public class ChromaDbStartWorker : IHostedService, IDisposable
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ChromaDbStartWorker> _logger;
        private Process? _chromaProcess;

        public ChromaDbStartWorker(IConfiguration config, ILogger<ChromaDbStartWorker> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Chroma DB Auto-Start Service is starting...");

            bool autoStart = _config.GetValue<bool>("ChromaDb:AutoStart", true);
            if (!autoStart)
            {
                _logger.LogInformation("Chroma DB Auto-Start is disabled in configuration.");
                return;
            }

            // Extract port from URL (e.g. http://localhost:8000)
            var chromaUrl = _config["ChromaDb:Url"] ?? "http://localhost:8000";
            int port = 8000;
            string host = "localhost";
            try
            {
                var uri = new Uri(chromaUrl);
                port = uri.Port;
                host = uri.Host;
            }
            catch { }

            // Check if port is already open
            if (IsPortOpen(host, port))
            {
                _logger.LogInformation("Chroma DB is already running on {Host}:{Port}.", host, port);
                return;
            }

            // Retrieve CLI path and persist path
            string cliPath = _config["ChromaDb:CliPath"] ?? @"C:\Users\vinhmt\AppData\Local\Packages\PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0\LocalCache\local-packages\Python313\Scripts\chroma.exe";
            string persistPath = _config["ChromaDb:PersistPath"] ?? "chroma";

            // If persistPath is relative, make it absolute relative to the solution directory
            if (!Path.IsPathRooted(persistPath))
            {
                // Current directory is the project directory during development
                persistPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), persistPath));
            }

            if (!File.Exists(cliPath))
            {
                _logger.LogWarning("Chroma DB executable was not found at: {Path}. Auto-start skipped.", cliPath);
                return;
            }

            try
            {
                _logger.LogInformation("Starting Chroma DB from: {Path} with data at: {DataPath}", cliPath, persistPath);
                var startInfo = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = $"run --path \"{persistPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                _chromaProcess = Process.Start(startInfo);
                
                // Wait a moment and check if it successfully bound to the port
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(1000, cancellationToken);
                    if (IsPortOpen(host, port))
                    {
                        _logger.LogInformation("Chroma DB successfully started and is listening on {Host}:{Port}.", host, port);
                        return;
                    }
                }
                _logger.LogWarning("Chroma DB process was launched but port {Port} is still not reachable.", port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Chroma DB process.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_chromaProcess != null && !_chromaProcess.HasExited)
            {
                _logger.LogInformation("Stopping Chroma DB process...");
                try
                {
                    _chromaProcess.Kill(true); // Kill process and its children
                    _logger.LogInformation("Chroma DB process stopped.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while stopping Chroma DB process.");
                }
            }
            return Task.CompletedTask;
        }

        private bool IsPortOpen(string host, int port, int timeoutMs = 500)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(timeoutMs);
                if (success)
                {
                    client.EndConnect(result);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _chromaProcess?.Dispose();
        }
    }
}
