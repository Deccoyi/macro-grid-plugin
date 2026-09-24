using MacroGrid.Plugin.Abstractions;

namespace HelloCSharp;

// #region variables
/// <summary>Publishes <c>hellocsharp.count</c>. The server calls <see cref="RunAsync"/> once and expects it to
/// run until the token is cancelled; the action calls <see cref="Increment"/> whenever it fires.</summary>
public sealed class GreetingCounter : IVariableProvider, IVariableCatalogSource
{
    private IVariableStore? _store;
    private int _count;

    public int Count => _count;

    public IEnumerable<VariableInfo> Describe() =>
    [
        new("hellocsharp.count", "How many greetings were sent", "3", "Hello"),
    ];

    public async Task RunAsync(IVariableStore store, CancellationToken cancellationToken)
    {
        _store = store;
        store.Set("hellocsharp.count", _count);
        try { await Task.Delay(Timeout.Infinite, cancellationToken); }
        catch (OperationCanceledException) { }
    }

    public void Increment()
    {
        var count = Interlocked.Increment(ref _count);
        _store?.Set("hellocsharp.count", count);
    }
}
// #endregion variables
