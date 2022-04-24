namespace ReduxDotnet;

/// <summary>
/// Reducer interface.
/// </summary>
/// <typeparam name="TStore">Type of application store.</typeparam>
/// <typeparam name="TAction">Type of action.</typeparam>
public interface IReducer<TStore, TAction>
{
    /// <summary>
    /// Reducer logic.
    /// </summary>
    /// <param name="store">An instance of application store.</param>
    /// <param name="action">Action instance.</param>
    /// <returns></returns>
    public TStore Invoke(TStore store, TAction action);
}
