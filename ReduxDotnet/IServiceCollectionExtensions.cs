using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        return self;
    }

    public static IServiceCollection AddReducer<TReducer>(
        this IServiceCollection self,
        Func<IServiceProvider, TReducer> factory,
        ServiceLifetime reducerLifetime = ServiceLifetime.Singleton)
        where TReducer : class, IReducer
    {
        self.Add(new ServiceDescriptor(typeof(TReducer), factory, reducerLifetime));
        self.AddReducerCore<TReducer>(reducerLifetime);
        return self;
    }

    public static IServiceCollection AddReducer<TReducer>(
        this IServiceCollection self,
        ServiceLifetime reducerLifetime = ServiceLifetime.Singleton)
        where TReducer : class, IReducer
    {
        self.Add(new ServiceDescriptor(typeof(TReducer), typeof(TReducer), reducerLifetime));
        self.AddReducerCore<TReducer>(reducerLifetime);
        return self;
    }

    private static void AddReducerCore<TReducer>(
        this IServiceCollection self, 
        ServiceLifetime reducerLifetime) where TReducer : class, IReducer
    {
        var descriptors = typeof(TReducer).GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IReducer<,>))
            .Select(x => new ServiceDescriptor(x, p => p.GetRequiredService<TReducer>(), reducerLifetime));
        foreach (var d in descriptors)
        {
            self.Add(d);
        }
    }
}
