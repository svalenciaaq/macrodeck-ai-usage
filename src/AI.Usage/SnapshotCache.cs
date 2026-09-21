using System.Diagnostics.CodeAnalysis;

namespace AI.Usage;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification =
        "SemaphoreSlim is used only for async coordination and its wait handle is never accessed."
)]
internal sealed class SnapshotCache<T>
    where T : class
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly TimeSpan _ttl;
    private readonly TimeSpan _retryAfterFailure;

    private T? _value;
    private DateTimeOffset _refreshAfter =
        DateTimeOffset.MinValue;

    public SnapshotCache(
        TimeSpan ttl,
        TimeSpan? retryAfterFailure = null)
    {
        _ttl = ttl;

        _retryAfterFailure =
            retryAfterFailure ??
            TimeSpan.FromSeconds(10);
    }

    public async Task<T?> GetAsync(
        Func<CancellationToken, Task<T?>> loader,
        CancellationToken cancellationToken = default)
    {
        var current = _value;

        if (
            current is not null &&
            DateTimeOffset.UtcNow < _refreshAfter
        )
        {
            return current;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            current = _value;

            if (
                current is not null &&
                DateTimeOffset.UtcNow < _refreshAfter
            )
            {
                return current;
            }

            T? fresh = null;

            try
            {
                fresh = await loader(cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                fresh = null;
            }

            if (fresh is not null)
            {
                _value = fresh;

                _refreshAfter =
                    DateTimeOffset.UtcNow + _ttl;

                return fresh;
            }

            if (_value is not null)
            {
                _refreshAfter =
                    DateTimeOffset.UtcNow +
                    _retryAfterFailure;

                return _value;
            }

            return null;
        }
        finally
        {
            _gate.Release();
        }
    }
}
