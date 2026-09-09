using KBlazor.Services;
using KBlazor.WasmSample;
using KBlazor.WasmSample.Data;
using KBlazor.WasmSample.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Seed data, shared for the app lifetime
builder.Services.AddSingleton(SeedData.Create());

// The three services KBlazor requires
builder.Services.AddScoped<IFlexTableSettings, InMemoryFlexTableSettings>();
builder.Services.AddSingleton<IListViewSettingStore, InMemoryListViewSettingStore>();
builder.Services.AddScoped<IEntityLookupProvider, InMemoryEntityLookupProvider>();

// Optional: show DateTimes in the browser's local time (exercises the JS interop path)
builder.Services.AddScoped<IClientTimeZoneProvider, BrowserTimeZoneProvider>();

// Deliberately no authentication services: FlexTable must cope without AuthenticationStateProvider.

await builder.Build().RunAsync();
