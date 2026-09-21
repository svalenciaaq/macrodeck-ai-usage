using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace AI.Usage;

internal sealed record ClaudeUsageSnapshot(
    RateWindow FiveHour,
    RateWindow Weekly,
    DateTimeOffset FetchedAt
);

internal sealed class ClaudeUsageService
{
    private readonly SnapshotCache<ClaudeUsageSnapshot> _cache =
        new(
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(10)
        );

    private DateTimeOffset _nextRemoteRefresh =
        DateTimeOffset.MinValue;

    public Task<ClaudeUsageSnapshot?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _cache.GetAsync(
            ReadFreshAsync,
            cancellationToken
        );
    }

    private async Task<ClaudeUsageSnapshot?> ReadFreshAsync(
        CancellationToken cancellationToken)
    {
        await RefreshClaudeUsageIfNeededAsync(
            cancellationToken
        );

        var home =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile
            );

        var path =
            Path.Combine(
                home,
                ".claude.json"
            );

        if (!File.Exists(path))
        {
            return null;
        }

        var json =
            await File.ReadAllTextAsync(
                path,
                cancellationToken
            );

        using var document =
            JsonDocument.Parse(json);

        var root = document.RootElement;

        if (
            !root.TryGetProperty(
                "cachedUsageUtilization",
                out var cached
            ) ||
            !cached.TryGetProperty(
                "utilization",
                out var utilization
            )
        )
        {
            return null;
        }

        var fiveHour =
            ParseWindow(
                utilization,
                "five_hour",
                300
            );

        var weekly =
            ParseWindow(
                utilization,
                "seven_day",
                10080
            );

        if (
            fiveHour is null ||
            weekly is null
        )
        {
            return null;
        }

        var fetchedAt =
            DateTimeOffset.UtcNow;

        if (
            cached.TryGetProperty(
                "fetchedAtMs",
                out var fetchedAtMs
            ) &&
            fetchedAtMs.TryGetInt64(
                out var milliseconds
            )
        )
        {
            fetchedAt =
                DateTimeOffset
                    .FromUnixTimeMilliseconds(
                        milliseconds
                    );
        }

        return new ClaudeUsageSnapshot(
            fiveHour,
            weekly,
            fetchedAt
        );
    }

    private async Task RefreshClaudeUsageIfNeededAsync(
        CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow < _nextRemoteRefresh)
        {
            return;
        }

        var executable = FindClaudeExecutable();

        if (executable is null)
        {
            _nextRemoteRefresh =
                DateTimeOffset.UtcNow.AddMinutes(1);

            return;
        }

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );

        timeout.CancelAfter(
            TimeSpan.FromSeconds(30)
        );

        var startInfo =
            new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add("/usage");

        using var process =
            new Process
            {
                StartInfo = startInfo
            };

        var started = false;

        try
        {
            process.Start();
            started = true;

            var stdoutTask =
                process.StandardOutput.ReadToEndAsync(
                    timeout.Token
                );

            var stderrTask =
                process.StandardError.ReadToEndAsync(
                    timeout.Token
                );

            await process.WaitForExitAsync(
                timeout.Token
            );

            await Task.WhenAll(
                stdoutTask,
                stderrTask
            );

            _nextRemoteRefresh =
                DateTimeOffset.UtcNow.AddMinutes(
                    process.ExitCode == 0
                        ? 5
                        : 1
                );
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _nextRemoteRefresh =
                DateTimeOffset.UtcNow.AddMinutes(1);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _nextRemoteRefresh =
                DateTimeOffset.UtcNow.AddMinutes(1);
        }
        finally
        {
            if (started)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(
                            entireProcessTree: true
                        );

                        await process.WaitForExitAsync(
                            CancellationToken.None
                        );
                    }
                }
                catch
                {
                    // Best-effort cleanup during shutdown or process failure.
                }
            }
        }
    }

    private static string? FindClaudeExecutable()
    {
        var path =
            Environment.GetEnvironmentVariable("PATH");

        if (!string.IsNullOrWhiteSpace(path))
        {
            foreach (
                var directory in
                path.Split(
                    Path.PathSeparator,
                    StringSplitOptions.RemoveEmptyEntries
                )
            )
            {
                var candidate =
                    Path.Combine(
                        directory,
                        "claude"
                    );

                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        var home =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile
            );

        var candidates = new[]
        {
            Path.Combine(
                home,
                ".local",
                "bin",
                "claude"
            ),
            Path.Combine(
                home,
                ".npm-global",
                "bin",
                "claude"
            ),
            "/usr/local/bin/claude",
            "/usr/bin/claude"
        };

        return candidates.FirstOrDefault(
            File.Exists
        );
    }

    internal static RateWindow? ParseWindow(
        JsonElement utilization,
        string name,
        int windowMinutes)
    {
        if (
            !utilization.TryGetProperty(
                name,
                out var window
            ) ||
            !window.TryGetProperty(
                "utilization",
                out var used
            ) ||
            !window.TryGetProperty(
                "resets_at",
                out var reset
            )
        )
        {
            return null;
        }

        var resetText =
            reset.GetString();

        if (
            string.IsNullOrWhiteSpace(resetText) ||
            !DateTimeOffset.TryParse(
                resetText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var resetTime
            )
        )
        {
            return null;
        }

        return new RateWindow(
            used.GetDouble(),
            windowMinutes,
            resetTime.ToUnixTimeSeconds()
        );
    }
}
