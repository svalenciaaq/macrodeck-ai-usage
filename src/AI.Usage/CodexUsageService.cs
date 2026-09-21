using System.Diagnostics;
using System.Text.Json;

namespace AI.Usage;

internal sealed record RateWindow(
    double UsedPercent,
    int WindowMinutes,
    long ResetsAt
);

internal sealed record CodexUsageSnapshot(
    RateWindow? FiveHour,
    RateWindow? Weekly,
    string PlanType,
    int ResetCredits,
    DateTimeOffset FetchedAt
);

internal sealed class CodexUsageService
{
    private readonly SnapshotCache<CodexUsageSnapshot> _cache =
        new(
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(10)
        );

    public Task<CodexUsageSnapshot?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return _cache.GetAsync(
            QueryAsync,
            cancellationToken
        );
    }

    private static async Task<CodexUsageSnapshot?> QueryAsync(
        CancellationToken cancellationToken)
    {
        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        var startInfo = new ProcessStartInfo
        {
            FileName = "codex",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("app-server");
        startInfo.ArgumentList.Add("--listen");
        startInfo.ArgumentList.Add("stdio://");

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            return null;
        }

        var stderrTask =
            process.StandardError.ReadToEndAsync(
                CancellationToken.None
            );

        try
        {
            await process.StandardInput.WriteLineAsync(
                """
                {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"clientInfo":{"name":"ai-usage-monitor","title":"AI Usage Monitor","version":"0.1.0"},"capabilities":{"experimentalApi":true}}}
                """
            );

            await process.StandardInput.FlushAsync(
                timeout.Token
            );

            var initializeResponse =
                await ReadResponseAsync(process, 1, timeout.Token);

            if (initializeResponse is null)
            {
                return null;
            }

            await process.StandardInput.WriteLineAsync(
                """
                {"jsonrpc":"2.0","method":"initialized"}
                """
            );

            await process.StandardInput.WriteLineAsync(
                """
                {"jsonrpc":"2.0","id":2,"method":"account/rateLimits/read","params":{}}
                """
            );

            await process.StandardInput.FlushAsync(
                timeout.Token
            );

            var response =
                await ReadResponseAsync(process, 2, timeout.Token);

            if (response is null)
            {
                return null;
            }

            return Parse(response.Value);
        }
        finally
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

            try
            {
                await stderrTask;
            }
            catch
            {
                // stderr is diagnostic only.
            }
        }
    }

    private static async Task<JsonElement?> ReadResponseAsync(
        Process process,
        int expectedId,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line =
                await process.StandardOutput.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(line);

                var root = document.RootElement;

                if (
                    root.TryGetProperty("id", out var id) &&
                    id.ValueKind == JsonValueKind.Number &&
                    id.GetInt32() == expectedId
                )
                {
                    return root.Clone();
                }
            }
            catch (JsonException)
            {
            }
        }
    }

    internal static CodexUsageSnapshot? Parse(JsonElement response)
    {
        if (!response.TryGetProperty("result", out var result))
        {
            return null;
        }

        if (!result.TryGetProperty("rateLimits", out var rateLimits))
        {
            return null;
        }

        RateWindow? fiveHour = null;
        RateWindow? weekly = null;

        foreach (var name in new[] { "primary", "secondary" })
        {
            if (!rateLimits.TryGetProperty(name, out var window))
            {
                continue;
            }

            if (window.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var usedPercent =
                window.GetProperty("usedPercent").GetDouble();

            var windowMinutes =
                window.GetProperty("windowDurationMins").GetInt32();

            var resetsAt =
                window.GetProperty("resetsAt").GetInt64();

            var parsed = new RateWindow(
                usedPercent,
                windowMinutes,
                resetsAt
            );

            if (windowMinutes == 300)
            {
                fiveHour = parsed;
            }

            if (windowMinutes == 10080)
            {
                weekly = parsed;
            }
        }

        var planType =
            rateLimits.TryGetProperty("planType", out var plan)
                ? plan.GetString() ?? "unknown"
                : "unknown";

        var resetCredits = 0;

        if (
            result.TryGetProperty(
                "rateLimitResetCredits",
                out var resetInfo
            ) &&
            resetInfo.TryGetProperty(
                "availableCount",
                out var available
            )
        )
        {
            resetCredits = available.GetInt32();
        }

        if (
            fiveHour is null ||
            weekly is null
        )
        {
            return null;
        }

        return new CodexUsageSnapshot(
            fiveHour,
            weekly,
            planType,
            resetCredits,
            DateTimeOffset.UtcNow
        );
    }
}
