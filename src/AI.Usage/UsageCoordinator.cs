namespace AI.Usage;

internal sealed class UsageCoordinator
{
    private readonly CodexUsageService _codexService = new();
    private readonly ClaudeUsageService _claudeService = new();
    private readonly GeminiUsageService _geminiService = new();

    private CodexUsageSnapshot? _codex;
    private ClaudeUsageSnapshot? _claude;
    private GeminiUsageSnapshot? _gemini;

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        if (_loop is not null)
        {
            return;
        }

        /*
         * Carga inicial.
         *
         * No devolvemos el control a Macro Deck hasta intentar
         * obtener los tres snapshots.
         */
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
            /*
             * El primer snapshot ya fue cargado en StartAsync.
             * Esperamos antes del siguiente ciclo.
             */
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

            if (value is not null)
            {
                Volatile.Write(
                    ref _claude,
                    value
                );
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
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

            if (value is not null)
            {
                Volatile.Write(
                    ref _codex,
                    value
                );
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
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

            if (value is not null)
            {
                Volatile.Write(
                    ref _gemini,
                    value
                );
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
        }
    }
}
