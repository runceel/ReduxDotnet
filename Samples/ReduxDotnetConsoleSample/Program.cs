using Microsoft.Extensions.DependencyInjection;
using Reactive.Bindings;
using ReduxDotnet;

var services = new ServiceCollection();
// init ReduxDotnet with initial status
services.AddReduxDotnet<AppState>(new AppState(0));
// Add reducers and effects
services.AddReducer<Reducers>();
services.AddSingleton<Effects>();

var provider = services.BuildServiceProvider();

// AppStore is IReactiveProperty<AppState>.
var store = provider.GetRequiredService<IReactiveProperty<AppState>>();
store.Subscribe(x =>
    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: Status was changed {x}."));

// Dispatcher and Effects
var dispatcher = provider.GetRequiredService<IDispatcher<AppState>>();
var effects = provider.GetRequiredService<Effects>();

// Dispatch actions
dispatcher.Dispatch(new IncrementAction());
dispatcher.Dispatch(new IncrementAction());
dispatcher.Dispatch(new DecrementAction());

// Async operation
await dispatcher.DispatchAsync(effects.IncrementLater());


// Define app state
record AppState(int Count);

// Define actions
record IncrementAction();
record DecrementAction();

// Define reducer
class Reducers :
    IReducer<AppState, IncrementAction>,
    IReducer<AppState, DecrementAction>
{
    public AppState Invoke(AppState store, IncrementAction action) => 
        store with { Count = store.Count + 1 };

    public AppState Invoke(AppState store, DecrementAction action) =>
        store with { Count = store.Count - 1 };
}

// Define effects (If you want to use async operation)
class Effects
{
    public EffectDelegate<AppState> IncrementLater() => async (d, _) =>
    {
        await Task.Delay(2000);
        d.Dispatch(new IncrementAction());
    };
}
