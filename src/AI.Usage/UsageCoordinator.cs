using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace AI.Usage;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification =
        "The CancellationTokenSource is created by StartAsync and disposed by StopAsync during integration shutdown."
)]
internal sealed class UsageCoordinator
{
    private readonly ILogger _logger;

    private readonly CodexUsageService _codexService = new();
    private readonly ClaudeUsageService _claudeService = new();
    private readonly GeminiUsageService _geminiService = new();

    private CodexUsageSnapshot? _codex;
    private ClaudeUsageSnapshot? _claude;
    private GeminiUsageSnapshot? _gemini;

    private CancellationTokenSource? _cts;
    private Task? _loop;

    private bool _codexFailed;
    private bool _claudeFailed;
    private bool _geminiFailed;

    public UsageCoordinator(ILogger logger)
    {
        _logger = logger.ForContext<UsageCoordinator>();
    }

    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        if (_loop is not null)
        {
            return;
        }

        // Load the initial snapshots before exposing the integration.
        await RefreshClaudeAsync(cancellationToken);
        await RefreshCodexAsync(cancellationToken);
        await RefreshGeminiAsync(cancellationToken);

        _cts = new CancellationTokenSource();

        _loop = Task.Run(
            () => RefreshLoopAsync(_cts.Token),
            CancellationToken.None
        );
    }

    public async Task StopAsync()
    {
        if (_cts is null || _loop is null)
        {
            return;
        }

        _cts.Cancel();

        try
        {
            await _loop;
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Dispose();

        _cts = null;
        _loop = null;
    }

    public ValueTask<CodexUsageSnapshot?> GetCodexAsync()
    {
        return ValueTask.FromResult(
            Volatile.Read(ref _codex)
        );
    }

    public ValueTask<ClaudeUsageSnapshot?> GetClaudeAsync()
    {
        return ValueTask.FromResult(
            Volatile.Read(ref _claude)
        );
    }

    public ValueTask<GeminiUsageSnapshot?> GetGeminiAsync()
    {
        return ValueTask.FromResult(
            Volatile.Read(ref _gemini)
        );
    }

    private async Task RefreshLoopAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(30),
                cancellationToken
            );

            await RefreshClaudeAsync(cancellationToken);

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken
            );

            await RefreshCodexAsync(cancellationToken);

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken
            );

            await RefreshGeminiAsync(cancellationToken);
        }
    }

    private async Task RefreshClaudeAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var value =
                await _claudeService.GetAsync(
                    cancellationToken
                );

            if (value is null)
            {
                MarkFailure(
                    ref _claudeFailed,
                    "Claude"
                );

                return;
            }

            Volatile.Write(
                ref _claude,
                value
            );

            MarkRecovery(
                ref _claudeFailed,
                "Claude"
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            MarkFailure(
                ref _claudeFailed,
                "Claude",
                ex
            );
        }
    }

    private async Task RefreshCodexAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var value =
                await _codexService.GetAsync(
                    cancellationToken
                );

            if (value is null)
            {
                MarkFailure(
                    ref _codexFailed,
                    "Codex"
                );

                return;
            }

            Volatile.Write(
                ref _codex,
                value
            );

            MarkRecovery(
                ref _codexFailed,
                "Codex"
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            MarkFailure(
                ref _codexFailed,
                "Codex",
                ex
            );
        }
    }

    private async Task RefreshGeminiAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var value =
                await _geminiService.GetAsync(
                    cancellationToken
                );

            if (value is null)
            {
                MarkFailure(
                    ref _geminiFailed,
                    "Gemini"
                );

                return;
            }

            Volatile.Write(
                ref _gemini,
                value
            );

            MarkRecovery(
                ref _geminiFailed,
                "Gemini"
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            MarkFailure(
                ref _geminiFailed,
                "Gemini",
                ex
            );
        }
    }

    private void MarkFailure(
        ref bool failed,
        string provider,
        Exception? exception = null)
    {
        if (failed)
        {
            return;
        }

        failed = true;

        if (exception is null)
        {
            _logger.Warning(
                "{Provider} usage refresh returned no data. Keeping the last valid snapshot.",
                provider
            );

            return;
        }

        _logger.Warning(
            exception,
            "{Provider} usage refresh failed. Keeping the last valid snapshot.",
            provider
        );
    }

    private void MarkRecovery(
        ref bool failed,
        string provider)
    {
        if (!failed)
        {
            return;
        }

        failed = false;

        _logger.Information(
            "{Provider} usage refresh recovered.",
            provider
        );
    }
}
