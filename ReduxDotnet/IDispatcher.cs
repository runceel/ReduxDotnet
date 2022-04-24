using System.Diagnostics;

namespace ReduxDotnet;

/// <summary>
/// Dispatcher
/// </summary>
/// <typeparam name="TStore">Type of store data.</typeparam>
public interface IDispatcher<TStore>
{
    ValueTask DispatchAsync<T>(T dispatchedValue);
}

public static class IDispatcherExtensions
{
    public static void Dispatch<TStore, T>(this IDispatcher<TStore> self, T dispatchedValue)
    {
        _ = self.DispatchAsync(dispatchedValue).AsTask().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.WriteLine(t.Exception);
            }
        });
    }
}
