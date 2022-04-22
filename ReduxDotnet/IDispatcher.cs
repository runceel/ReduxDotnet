namespace ReduxDotnet;

/// <summary>
/// Dispatcher
/// </summary>
/// <typeparam name="TStore">Type of store data.</typeparam>
public interface IDispatcher<TStore>
{
    void Dispatch<TAction>(TAction action);
    ValueTask DispatchAsync(EffectDelegate<TStore> effect);
}
