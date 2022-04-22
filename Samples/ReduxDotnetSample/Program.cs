using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReduxDotnetSample;
using ReduxDotnet;
using ReduxDotnetSample.Store;
using ReduxDotnetSample.Reducers;
using ReduxDotnetSample.Effects;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddReduxDotnet(new AppState(0));
builder.Services.AddReducer<AppReducers>();
builder.Services.AddSingleton<AppEffects>();

await builder.Build().RunAsync();
