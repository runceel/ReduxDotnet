using Microsoft.Extensions.DependencyInjection;
using Reactive.Bindings;

namespace ReduxDotnet;

/// <summary>
/// IServiceCollection extension methods for ReduxDotnet.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddReduxDotnet<TStore>(
        this IServiceCollection self,
        TStore initialState)
    {
        self.AddSingleton<IReactiveProperty<TStore>>(
            new ReactivePropertySlim<TStore>(initialState));
        self.AddSingleton<IDispatcher<TStore>, Dispatcher<TStore>>();
        self.AddSingleton<IDispatcherHandler<TStore>, EffectHandler<TStore>>();
        return self;
    }

    public static IServiceCollection AddReducer<TStore, TReducer>(
        this IServiceCollection self,
        Func<IServiceProvider, TReducer> factory,
        ServiceLifetime reducerLifetime = ServiceLifetime.Singleton)
        where TReducer : class
    {
        self.Add(new ServiceDescriptor(typeof(TReducer), factory, reducerLifetime));
        self.AddReducerCore<TStore, TReducer>(reducerLifetime);
        return self;
    }

    public static IServiceCollection AddReducer<TStore, TReducer>(
        this IServiceCollection self,
        ServiceLifetime reducerLifetime = ServiceLifetime.Singleton)
        where TReducer : class
    {
        self.Add(new ServiceDescriptor(typeof(TReducer), typeof(TReducer), reducerLifetime));
        self.AddReducerCore<TStore, TReducer>(reducerLifetime);
        return self;
    }

    private static void AddReducerCore<TStore, TReducer>(
        this IServiceCollection self,
        ServiceLifetime reducerLifetime) where TReducer : class
    {
        self.AddSingleton<IDispatcherHandler<TStore>, ActionHandler<TStore, TReducer>>();
    }
}
