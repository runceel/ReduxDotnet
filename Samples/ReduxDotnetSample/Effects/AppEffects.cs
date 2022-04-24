using ReduxDotnet;
using ReduxDotnetSample.Actions;
using ReduxDotnetSample.Store;

namespace ReduxDotnetSample.Effects;

public class AppEffects
{
    public EffectDelegate<AppState> IncrementAsync(int amount) => async dispatcher =>
    {
        await Task.Delay(2000);
        await dispatcher.DispatchAsync(new IncrementAction(amount));
    };
}
