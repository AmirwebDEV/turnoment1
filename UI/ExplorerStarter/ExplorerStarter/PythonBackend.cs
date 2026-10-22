using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ExplorerStarter
{
    public sealed class PythonBackend : IDisposable
    {
        private readonly Process process;
        private readonly SemaphoreSlim requestLock = new(1, 1);

        public PythonBackend(string pythonExe, string scriptPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process = new Process { StartInfo = startInfo };
            process.Start();

            // Drain stderr so Python errors cannot block the process.
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    System.Diagnostics.Debug.WriteLine(
                        "[Python] " + e.Data);
            };

            process.BeginErrorReadLine();
        }

        public async Task<JsonDocument> SendAsync(object request)
        {
            await requestLock.WaitAsync();

            try
            {
                if (process.HasExited)
                    throw new InvalidOperationException(
                        "The Python backend has stopped.");

                string json = JsonSerializer.Serialize(request);

                await process.StandardInput.WriteLineAsync(json);
                await process.StandardInput.FlushAsync();

                string? response =
                    await process.StandardOutput.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(response))
                    throw new InvalidOperationException(
                        "Python returned an empty response.");

                return JsonDocument.Parse(response);
            }
            finally
            {
                requestLock.Release();
            }
        }

        public void Dispose()
        {
            try
            {
                if (!process.HasExited)
                {
                    process.StandardInput.Close();

                    if (!process.WaitForExit(1500))
                        process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // The process may already have exited.
            }

            process.Dispose();
            requestLock.Dispose();
        }
    }
}
