using Reactive.Bindings;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Linq.Expressions;

namespace ReduxDotnet;
public class Dispatcher<TStore> : IDispatcher<TStore>
{
    private readonly IReactiveProperty<TStore> _state;
    private readonly IEnumerable<IDispatcherHandler<TStore>> _handlers;
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IReactiveProperty<TStore> initialState,
        IEnumerable<IDispatcherHandler<TStore>> handlers,
        IServiceProvider serviceProvider)
    {
        _state = initialState;
        _handlers = handlers;
        _serviceProvider = serviceProvider;

        foreach (var h in _handlers)
        {
            h.Initialize(this);
        }
    }

    public async ValueTask DispatchAsync<T>(T dispatchedValue)
    {
        foreach (var h in _handlers)
        {
            var r = await h.InvokeAsync(_state.Value, dispatchedValue);
            if (r.Handled)
            {
                if (r.Result.StoreWasUpdated)
                {
                    _state.Value = r.Result.Store;
                }
            }
        }
    }
}

public interface IDispatcherHandler<TStore>
{
    void Initialize(IDispatcher<TStore> dispatcher);
    ValueTask<(bool Handled, DispatcherHandlerResult<TStore> Result)> InvokeAsync(TStore store, object dispatchedValue);
}

public abstract class DispatcherHandler<TStore> : IDispatcherHandler<TStore>
{
    protected IDispatcher<TStore> Dispatcher { get; private set; }
    public ValueTask<(bool Handled, DispatcherHandlerResult<TStore> Result)> InvokeAsync(TStore store, object dispatchedValue)
    {
        if (!IsTargetValue(dispatchedValue))
            return ValueTask.FromResult((false, default(DispatcherHandlerResult<TStore>)));

        return InvokeCoreAsync(store, dispatchedValue);
    }

    protected abstract bool IsTargetValue(object dispatchedValue);

    protected abstract ValueTask<(bool Handled, DispatcherHandlerResult<TStore> Result)> InvokeCoreAsync(TStore store, object dispatchedValue);

    public void Initialize(IDispatcher<TStore> dispatcher)
    {
        Dispatcher = dispatcher;
    }
}

public abstract class DispatcherHandler<TStore, TDispatchedValue> : DispatcherHandler<TStore>
{
    protected override bool IsTargetValue(object dispatchedValue) => dispatchedValue is TDispatchedValue;

    protected override ValueTask<(bool Handled, DispatcherHandlerResult<TStore> Result)> InvokeCoreAsync(TStore store, object dispatchedValue) =>
        InvokeCoreAsync(store, (TDispatchedValue)dispatchedValue);

    protected abstract ValueTask<(bool Handled, DispatcherHandlerResult<TStore> Result)> InvokeCoreAsync(TStore store, TDispatchedValue dispatchedValue);
}

public struct DispatcherHandlerResult<TStore>
{
    [MemberNotNullWhen(true, nameof(Store))]
    public bool StoreWasUpdated { get; }
    public TStore? Store { get; }

    public DispatcherHandlerResult() : this(false, default)
    {
    }

    public DispatcherHandlerResult(bool storeWasUpdated, TStore? store)
    {
        StoreWasUpdated = storeWasUpdated;
        if (StoreWasUpdated && store is null) throw new ArgumentNullException(nameof(store));
        Store = store;
    }
}

class ActionHandler<TStore, TReducer> : DispatcherHandler<TStore>
    where TReducer : class
{
    private static readonly Dictionary<Type, (bool IsTarget, Func<TReducer, TStore, object, TStore>? CallReducer)> _cache = new();
    private readonly TReducer _reducer;

    public ActionHandler(TReducer reducer)
    {
        _reducer = reducer;
    }

    public ValueTask<DispatcherHandlerResult<TStore>> InvokeAsync(TStore store, object dispatchedValue) =>
        ValueTask.FromResult(Invoke(store, dispatchedValue));

    private DispatcherHandlerResult<TStore> Invoke(TStore store, object dispatchedValue)
    {
        var dispatchedValueType = dispatchedValue.GetType();
        var reducerInfo = GetOrAddReducerInfo(dispatchedValueType);
        if (!reducerInfo.IsTarget)
        {
            return new(false, default);
        }

        return new(true, reducerInfo.CallReducer!(_reducer, store, dispatchedValue));
    }

    private (bool IsTarget, Func<TReducer, TStore, object, TStore>? CallReducer) GetOrAddReducerInfo(Type dispatchedValueType)
    {
        lock (_cache)
        {
            if (_cache.ContainsKey(dispatchedValueType))
                return _cache[dispatchedValueType];

            var reducerType = typeof(IReducer<,>).MakeGenericType(typeof(TStore), dispatchedValueType);
            var target = typeof(TReducer).IsAssignableTo(reducerType);
            if (!target)
            {
                _cache.Add(dispatchedValueType, (false, null));
                return (false, null);
            }

            var invokeMethod = reducerType.GetMethod("Invoke");
            if (invokeMethod == null)
            {
                _cache.Add(dispatchedValueType, (false, null));
                return (false, null);
            }

            var reducerArgument = Expression.Parameter(typeof(TReducer), "reducer");
            var storeArgument = Expression.Parameter(typeof(TStore), "store");
            var actionArgument = Expression.Parameter(typeof(object), "action");

            var reducerVar = Expression.Variable(reducerType, "typedReducer");
            var actionVar = Expression.Variable(dispatchedValueType, "typedAction");
            var resultVar = Expression.Variable(typeof(TStore), "result");

            var block = Expression.Block(
                new[] { reducerVar, actionVar, resultVar },
                new Expression[]
                {
                    Expression.Assign(reducerVar, Expression.Convert(reducerArgument, reducerType)),
                    Expression.Assign(actionVar, Expression.Convert(actionArgument, dispatchedValueType)),
                    Expression.Assign(resultVar, Expression.Call(reducerVar, invokeMethod, storeArgument, actionVar)),
                    resultVar,
                });
            var func = Expression.Lambda<Func<TReducer, TStore, object, TStore>>(
                block,
                reducerArgument,
                storeArgument,
                actionArgument)
                .Compile();

            _cache.Add(dispatchedValueType, (true, func));
            return (true, func);
        }
    }

    protected override bool IsTargetValue(object dispatchedValue)
    {
        return GetOrAddReducerInfo(dispatchedValue.GetType()).IsTarget;
    }

    protected override ValueTask<(bool Handled, DispatcherHandlerResult<TStore> Result)> InvokeCoreAsync(TStore store, object dispatchedValue)
    {
        (_, var invoker) = GetOrAddReducerInfo(dispatchedValue.GetType());
        return ValueTask.FromResult((
            true, 
            new DispatcherHandlerResult<TStore>(true, invoker!.Invoke(_reducer, store, dispatchedValue))));
    }
}

class EffectHandler<TStore> : DispatcherHandler<TStore, EffectDelegate<TStore>>
{
    public async ValueTask<DispatcherHandlerResult<TStore>> InvokeAsync(TStore store, EffectDelegate<TStore> dispatchedValue)
    {
        await dispatchedValue(Dispatcher);
        return new(false, default(TStore));
    }

    protected override async ValueTask<(bool Handled, DispatcherHandlerResult<TStore> Result)> InvokeCoreAsync(
        TStore store,
        EffectDelegate<TStore> dispatchedValue)
    {
        await dispatchedValue(Dispatcher);
        return (true, new(false, default));
    }
}
