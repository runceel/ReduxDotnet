using ReduxDotnet;
using ReduxDotnetSample.Actions;
using ReduxDotnetSample.Store;

namespace ReduxDotnetSample.Effects;

public class AppEffects
{
    public EffectDelegate<AppState> IncrementAsync(int amount) => async (dispatcher, _) =>
    {
        await Task.Delay(2000);
        dispatcher.Dispatch(new IncrementAction(amount));
    };
}
