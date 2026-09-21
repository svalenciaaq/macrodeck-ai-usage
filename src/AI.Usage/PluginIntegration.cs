using System.Globalization;
using MacroDeck.Sdk;
using MacroDeck.Sdk.Actions;
using MacroDeck.Sdk.Variables;
using Serilog;

namespace AI.Usage;

public sealed class PluginIntegration :
    IPluginIntegration,
    IVariableProvider
{
    private readonly ILogger _logger;

    private readonly UsageCoordinator _usage = new();

    public PluginIntegration(ILogger logger)
    {
        _logger = logger.ForContext<PluginIntegration>();

        Actions =
        [
            new LogMessageAction(logger)
        ];
    }

    public IReadOnlyList<IActionDefinition> Actions { get; }

    public IReadOnlyList<VariableDefinition> Variables { get; } =
    [
        Text(
            "ai_usage_codex_5h_card",
            "codex-5h-card"
        ),

        Text(
            "ai_usage_codex_week_card",
            "codex-week-card"
        ),

        Text(
            "ai_usage_claude_5h_card",
            "claude-5h-card"
        ),

        Text(
            "ai_usage_claude_week_card",
            "claude-week-card"
        ),

        Text(
            "ai_usage_gemini_5h_card",
            "gemini-5h-card"
        ),

        Text(
            "ai_usage_gemini_week_card",
            "gemini-week-card"
        )
    ];

    private static VariableDefinition Numeric(
        string name,
        string id)
    {
        return VariableDefinition.Eager(
            name,
            VariableType.Numeric,
            refreshInterval: TimeSpan.FromSeconds(30)
        ) with
        {
            Id = id,
            Unit = "%",
            SemanticKind = VariableSemanticKinds.Percentage
        };
    }

    private static VariableDefinition Text(
        string name,
        string id)
    {
        return VariableDefinition.Eager(
            name,
            VariableType.Text,
            refreshInterval: TimeSpan.FromSeconds(30)
        ) with
        {
            Id = id
        };
    }

    public async ValueTask<VariableReading> ReadAsync(
        string localId,
        CancellationToken cancellationToken = default)
    {
        if (localId.StartsWith(
            "codex-",
            StringComparison.Ordinal))
        {
            return await ReadCodexAsync(
                localId,
                cancellationToken
            );
        }

        if (localId.StartsWith(
            "claude-",
            StringComparison.Ordinal))
        {
            return await ReadClaudeAsync(
                localId,
                cancellationToken
            );
        }

        if (localId.StartsWith(
            "gemini-",
            StringComparison.Ordinal))
        {
            return await ReadGeminiAsync(
                localId,
                cancellationToken
            );
        }

        return VariableReading.Unavailable;
    }

    private async ValueTask<VariableReading> ReadCodexAsync(
        string localId,
        CancellationToken cancellationToken)
    {
        var usage =
            await _usage.GetCodexAsync();

        if (usage is null)
        {
            return VariableReading.Unavailable;
        }

        return localId switch
        {
            "codex-5h-remaining" =>
                Remaining(usage.FiveHour),

            "codex-5h-used" =>
                Used(usage.FiveHour),

            "codex-week-remaining" =>
                Remaining(usage.Weekly),

            "codex-week-used" =>
                Used(usage.Weekly),

            "codex-5h-reset" =>
                ResetTime(usage.FiveHour),

            "codex-week-reset" =>
                ResetTime(usage.Weekly),

            "codex-5h-reset-in" =>
                ResetIn(usage.FiveHour),

            "codex-week-reset-in" =>
                ResetIn(usage.Weekly),

            "codex-5h-bar" =>
                Bar(usage.FiveHour),

            "codex-week-bar" =>
                Bar(usage.Weekly),

            "codex-5h-status" =>
                Status(usage.FiveHour),

            "codex-week-status" =>
                Status(usage.Weekly),

            "codex-plan" =>
                VariableReading.Of(usage.PlanType),

            "codex-reset-credits" =>
                VariableReading.Of(usage.ResetCredits),

            "codex-5h-card" =>
                Card(usage.FiveHour),

            "codex-week-card" =>
                Card(usage.Weekly),

            _ =>
                VariableReading.Unavailable
        };
    }

    private async ValueTask<VariableReading> ReadClaudeAsync(
        string localId,
        CancellationToken cancellationToken)
    {
        var usage =
            await _usage.GetClaudeAsync();

        if (usage is null)
        {
            return VariableReading.Unavailable;
        }

        return localId switch
        {
            "claude-5h-remaining" =>
                Remaining(usage.FiveHour),

            "claude-5h-used" =>
                Used(usage.FiveHour),

            "claude-week-remaining" =>
                Remaining(usage.Weekly),

            "claude-week-used" =>
                Used(usage.Weekly),

            "claude-5h-reset" =>
                ResetTime(usage.FiveHour),

            "claude-week-reset" =>
                ResetTime(usage.Weekly),

            "claude-5h-reset-in" =>
                ResetIn(usage.FiveHour),

            "claude-week-reset-in" =>
                ResetIn(usage.Weekly),

            "claude-5h-bar" =>
                Bar(usage.FiveHour),

            "claude-week-bar" =>
                Bar(usage.Weekly),

            "claude-5h-status" =>
                Status(usage.FiveHour),

            "claude-week-status" =>
                Status(usage.Weekly),

            "claude-last-update" =>
                VariableReading.Of(
                    usage.FetchedAt
                        .ToLocalTime()
                        .ToString(
                            "HH:mm",
                            CultureInfo.InvariantCulture
                        )
                ),

            "claude-5h-card" =>
                Card(usage.FiveHour),

            "claude-week-card" =>
                Card(usage.Weekly),

            _ =>
                VariableReading.Unavailable
        };
    }

    private async ValueTask<VariableReading> ReadGeminiAsync(
        string localId,
        CancellationToken cancellationToken)
    {
        var usage =
            await _usage.GetGeminiAsync();

        if (usage is null)
        {
            return VariableReading.Unavailable;
        }

        return localId switch
        {
            "gemini-5h-remaining" =>
                Remaining(usage.FiveHour),

            "gemini-5h-used" =>
                Used(usage.FiveHour),

            "gemini-week-remaining" =>
                Remaining(usage.Weekly),

            "gemini-week-used" =>
                Used(usage.Weekly),

            "gemini-5h-reset" =>
                ResetTime(usage.FiveHour),

            "gemini-week-reset" =>
                ResetTime(usage.Weekly),

            "gemini-5h-reset-in" =>
                ResetIn(usage.FiveHour),

            "gemini-week-reset-in" =>
                ResetIn(usage.Weekly),

            "gemini-5h-bar" =>
                Bar(usage.FiveHour),

            "gemini-week-bar" =>
                Bar(usage.Weekly),

            "gemini-5h-status" =>
                Status(usage.FiveHour),

            "gemini-week-status" =>
                Status(usage.Weekly),

            "gemini-last-update" =>
                VariableReading.Of(
                    usage.FetchedAt
                        .ToLocalTime()
                        .ToString(
                            "HH:mm",
                            CultureInfo.InvariantCulture
                        )
                ),

            "gemini-5h-card" =>
                Card(usage.FiveHour),

            "gemini-week-card" =>
                Card(usage.Weekly),

            _ =>
                VariableReading.Unavailable
        };
    }

    private static double RemainingValue(
        RateWindow window)
    {
        return Math.Clamp(
            100.0 - window.UsedPercent,
            0,
            100
        );
    }

    private static VariableReading Remaining(
        RateWindow? window)
    {
        return window is null
            ? VariableReading.Unavailable
            : VariableReading.Of(
                Math.Round(RemainingValue(window))
            );
    }

    private static VariableReading Used(
        RateWindow? window)
    {
        return window is null
            ? VariableReading.Unavailable
            : VariableReading.Of(
                Math.Round(window.UsedPercent)
            );
    }

    private static VariableReading ResetTime(
        RateWindow? window)
    {
        if (window is null)
        {
            return VariableReading.Unavailable;
        }

        var value =
            DateTimeOffset
                .FromUnixTimeSeconds(
                    window.ResetsAt
                )
                .ToLocalTime()
                .ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture
                );

        return VariableReading.Of(value);
    }

    private static VariableReading ResetIn(
        RateWindow? window)
    {
        if (window is null)
        {
            return VariableReading.Unavailable;
        }

        var reset =
            DateTimeOffset.FromUnixTimeSeconds(
                window.ResetsAt
            );

        var remaining =
            reset - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            return VariableReading.Of("ahora");
        }

        if (remaining.TotalDays >= 1)
        {
            var days =
                (int)remaining.TotalDays;

            return VariableReading.Of(
                $"{days}d {remaining.Hours}h"
            );
        }

        if (remaining.TotalHours >= 1)
        {
            return VariableReading.Of(
                $"{(int)remaining.TotalHours}h " +
                $"{remaining.Minutes}m"
            );
        }

        return VariableReading.Of(
            $"{Math.Max(1, remaining.Minutes)}m"
        );
    }

    private static VariableReading Card(
        RateWindow? window)
    {
        if (window is null)
        {
            return VariableReading.Unavailable;
        }

        var remaining =
            Math.Round(RemainingValue(window));

        const int cells = 10;

        var filled =
            (int)Math.Round(
                RemainingValue(window) / 100.0 * cells,
                MidpointRounding.AwayFromZero
            );

        filled =
            Math.Clamp(filled, 0, cells);

        var bar =
            new string('█', filled) +
            new string('░', cells - filled);

        var reset =
            DateTimeOffset.FromUnixTimeSeconds(
                window.ResetsAt
            );

        var left =
            reset - DateTimeOffset.UtcNow;

        string resetIn;

        if (left <= TimeSpan.Zero)
        {
            resetIn = "ahora";
        }
        else if (left.TotalDays >= 1)
        {
            resetIn =
                $"{(int)left.TotalDays}d {left.Hours}h";
        }
        else if (left.TotalHours >= 1)
        {
            resetIn =
                $"{(int)left.TotalHours}h {left.Minutes}m";
        }
        else
        {
            resetIn =
                $"{Math.Max(1, left.Minutes)}m";
        }

        return VariableReading.Of(
            $"{remaining:0}\n{bar}\n{resetIn}"
        );
    }

    private static VariableReading Bar(
        RateWindow? window)
    {
        if (window is null)
        {
            return VariableReading.Unavailable;
        }

        var remaining =
            RemainingValue(window);

        const int cells = 10;

        var filled =
            (int)Math.Round(
                remaining / 100.0 * cells,
                MidpointRounding.AwayFromZero
            );

        filled =
            Math.Clamp(
                filled,
                0,
                cells
            );

        var bar =
            new string('█', filled) +
            new string('░', cells - filled);

        return VariableReading.Of(bar);
    }

    private static VariableReading Status(
        RateWindow? window)
    {
        if (window is null)
        {
            return VariableReading.Unavailable;
        }

        var remaining =
            RemainingValue(window);

        var status =
            remaining switch
            {
                > 50 => "OK",
                > 20 => "MEDIO",
                _ => "BAJO"
            };

        return VariableReading.Of(status);
    }

    public async Task InitializeAsync(
        IIntegrationContext context)
    {
        _logger.Information(
            "Loading initial AI usage snapshots."
        );

        await _usage.StartAsync();

        _logger.Information(
            "AI Usage integration initialized."
        );
    }

    public Task ShutdownAsync()
    {
        return _usage.StopAsync();
    }
}
