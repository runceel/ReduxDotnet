using ReduxDotnet;
using ReduxDotnetSample.Actions;
using ReduxDotnetSample.Store;

namespace ReduxDotnetSample.Reducers;

public class AppReducers : 
    IReducer<AppState, IncrementAction>,
    IReducer<AppState, DecrementAction>
{
    public AppState Invoke(AppState store, IncrementAction action) => 
        store with { Count = store.Count + action.Amount };

    public AppState Invoke(AppState store, DecrementAction action) => 
        store with { Count = store.Count - action.Amount };
}
