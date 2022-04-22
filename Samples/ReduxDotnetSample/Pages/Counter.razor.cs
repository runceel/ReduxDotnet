using Microsoft.AspNetCore.Components;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ReduxDotnet;
using ReduxDotnetSample.Actions;
using ReduxDotnetSample.Effects;
using ReduxDotnetSample.Store;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ReduxDotnetSample.Pages;

public partial class Counter : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    [Inject]
    public IReactiveProperty<AppState> Store { get; set; } = default!;
    [Inject]
    public IDispatcher<AppState> Dispatcher { get; set; } = default!;
    [Inject]
    public AppEffects AppEffects { get; set; } = default!;

    public int CurrentCount => Store.Value.Count;

    private int Amount { get; set; } = 1;

    protected override void OnInitialized()
    {
        Store.Select(x => x.Count)
            .DistinctUntilChanged()
            .Subscribe(x =>
            {
                _ = InvokeAsync(() => StateHasChanged());
            })
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();

    private void IncrementCount()
    {
        Dispatcher.Dispatch(new IncrementAction(Amount));
    }
    private void DecrementCount()
    {
        Dispatcher.Dispatch(new DecrementAction(Amount));
    }

    private async Task IncrementCountAfterTwoSecondsAsync()
    {
        await Dispatcher.DispatchAsync(AppEffects.IncrementAsync(Amount));
    }
}
