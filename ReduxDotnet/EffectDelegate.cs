namespace ReduxDotnet;

public delegate ValueTask EffectDelegate<TStore>(IDispatcher<TStore> dispatcher, Func<TStore> getStore);
