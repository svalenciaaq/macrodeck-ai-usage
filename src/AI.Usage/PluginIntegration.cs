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
    private readonly UsageCoordinator _usage;

    public PluginIntegration(ILogger logger)
    {
        _logger = logger.ForContext<PluginIntegration>();
        _usage = new UsageCoordinator(logger);
    }

    public IReadOnlyList<IActionDefinition> Actions { get; } = [];

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
        cancellationToken.ThrowIfCancellationRequested();

        if (localId.StartsWith(
            "codex-",
            StringComparison.Ordinal))
        {
            return await ReadCodexAsync(localId);
        }

        if (localId.StartsWith(
            "claude-",
            StringComparison.Ordinal))
        {
            return await ReadClaudeAsync(localId);
        }

        if (localId.StartsWith(
            "gemini-",
            StringComparison.Ordinal))
        {
            return await ReadGeminiAsync(localId);
        }

        return VariableReading.Unavailable;
    }

    private async ValueTask<VariableReading> ReadCodexAsync(
        string localId)
    {
        var usage = await _usage.GetCodexAsync();

        if (usage is null)
        {
            return VariableReading.Unavailable;
        }

        return localId switch
        {
            "codex-5h-card" =>
                Card(usage.FiveHour),

            "codex-week-card" =>
                Card(usage.Weekly),

            _ =>
                VariableReading.Unavailable
        };
    }

    private async ValueTask<VariableReading> ReadClaudeAsync(
        string localId)
    {
        var usage = await _usage.GetClaudeAsync();

        if (usage is null)
        {
            return VariableReading.Unavailable;
        }

        return localId switch
        {
            "claude-5h-card" =>
                Card(usage.FiveHour),

            "claude-week-card" =>
                Card(usage.Weekly),

            _ =>
                VariableReading.Unavailable
        };
    }

    private async ValueTask<VariableReading> ReadGeminiAsync(
        string localId)
    {
        var usage = await _usage.GetGeminiAsync();

        if (usage is null)
        {
            return VariableReading.Unavailable;
        }

        return localId switch
        {
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
            resetIn = "0m";
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
