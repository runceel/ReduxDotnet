using Reactive.Bindings;
using Microsoft.Extensions.DependencyInjection;

namespace ReduxDotnet;
public class Dispatcher<TStore> : IDispatcher<TStore>
{
    private readonly IReactiveProperty<TStore> _state;
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IReactiveProperty<TStore> initialState, 
        IServiceProvider serviceProvider)
    {
        _state = initialState;
        _serviceProvider = serviceProvider;
    }

    public void Dispatch<TAction>(TAction action)
    {
        var reducer = _serviceProvider.GetRequiredService<IReducer<TStore, TAction>>();
        _state.Value = reducer.Invoke(_state.Value, action);
    }

    public async ValueTask DispatchAsync(EffectDelegate<TStore> effect)
    {
        await effect(this, () => _state.Value);
    }
}
