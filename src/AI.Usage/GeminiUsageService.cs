using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace AI.Usage;

internal sealed record GeminiUsageSnapshot(
    RateWindow FiveHour,
    RateWindow Weekly,
    DateTimeOffset FetchedAt
);

internal sealed class GeminiUsageService
{
    private readonly SnapshotCache<GeminiUsageSnapshot> _cache =
        new(
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(10)
        );

    public Task<GeminiUsageSnapshot?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _cache.GetAsync(
            QueryAsync,
            cancellationToken
        );
    }

    private static async Task<GeminiUsageSnapshot?> QueryAsync(
        CancellationToken cancellationToken)
    {
        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );

        timeout.CancelAfter(
            TimeSpan.FromSeconds(10)
        );

        var startInfo =
            new ProcessStartInfo
            {
                FileName = "agy",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        startInfo.ArgumentList.Add("--print");
        startInfo.ArgumentList.Add("/usage");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("json");

        startInfo.Environment[
            "AGY_CLI_DISABLE_AUTO_UPDATE"
        ] = "true";

        using var process =
            new Process
            {
                StartInfo = startInfo
            };

        var started = false;
        string stdout;

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

            stdout = await stdoutTask;
            _ = await stderrTask;

            if (process.ExitCode != 0)
            {
                return null;
            }
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

        return Parse(
            stdout,
            DateTimeOffset.UtcNow
        );
    }

    internal static GeminiUsageSnapshot? Parse(
        string json,
        DateTimeOffset fetchedAt)
    {
        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        if (
            !root.TryGetProperty(
                "command",
                out var command
            ) ||
            !command.TryGetProperty(
                "data",
                out var data
            ) ||
            !data.TryGetProperty(
                "groups",
                out var groups
            )
        )
        {
            return null;
        }

        foreach (
            var group in
            groups.EnumerateArray()
        )
        {
            var name =
                group.TryGetProperty(
                    "name",
                    out var nameElement
                )
                    ? nameElement.GetString()
                    : null;

            if (
                !string.Equals(
                    name,
                    "Gemini Models",
                    StringComparison.Ordinal
                )
            )
            {
                continue;
            }

            if (
                !group.TryGetProperty(
                    "buckets",
                    out var buckets
                )
            )
            {
                return null;
            }

            RateWindow? fiveHour = null;
            RateWindow? weekly = null;

            foreach (
                var bucket in
                buckets.EnumerateArray()
            )
            {
                if (
                    !bucket.TryGetProperty(
                        "window",
                        out var windowElement
                    ) ||
                    !bucket.TryGetProperty(
                        "remaining_fraction",
                        out var remainingElement
                    ) ||
                    !bucket.TryGetProperty(
                        "reset_time",
                        out var resetElement
                    )
                )
                {
                    continue;
                }

                var window =
                    windowElement.GetString();

                var remainingFraction =
                    remainingElement.GetDouble();

                var resetText =
                    resetElement.GetString();

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
                    continue;
                }

                var usedPercent =
                    Math.Clamp(
                        100.0 -
                        remainingFraction * 100.0,
                        0,
                        100
                    );

                if (window == "5h")
                {
                    fiveHour =
                        new RateWindow(
                            usedPercent,
                            300,
                            resetTime.ToUnixTimeSeconds()
                        );
                }

                if (window == "weekly")
                {
                    weekly =
                        new RateWindow(
                            usedPercent,
                            10080,
                            resetTime.ToUnixTimeSeconds()
                        );
                }
            }

            if (
                fiveHour is null ||
                weekly is null
            )
            {
                return null;
            }

            return new GeminiUsageSnapshot(
                fiveHour,
                weekly,
                fetchedAt
            );
        }

        return null;
    }

}
