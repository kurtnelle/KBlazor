// KBlazor.Showcase/Program.cs
using KBlazor.Services;
using KBlazor.Showcase.Data;
using KBlazor.Showcase.Endpoints;
using KBlazor.Showcase.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();

// KBlazor required services
builder.Services.AddScoped<IFlexTableSettings, InMemoryFlexTableSettings>();
builder.Services.AddSingleton<IListViewSettingStore, InMemoryListViewSettingStore>();
builder.Services.AddScoped<IEntityLookupProvider, InMemoryEntityLookupProvider>();

// Singleton data store — seeded once at startup
builder.Services.AddSingleton(SeedData.Create());

// NuGet feed service
builder.Services.AddSingleton<NuGetFeedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapNuGetFeedEndpoints();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
