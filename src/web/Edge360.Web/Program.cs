using Blazored.LocalStorage;
using Edge360.Web;
using Edge360.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL is configured per deployment (wwwroot/appsettings.json), defaulting to the dev API.
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5092";

builder.Services.AddBlazoredLocalStorage();

// Auth + state
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<MapInterop>();

// Plain client for anonymous auth endpoints.
builder.Services.AddHttpClient<AuthClient>(c => c.BaseAddress = new Uri(apiBase));

// Authenticated API client (bearer + refresh handler).
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddHttpClient<ApiClient>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<AuthHeaderHandler>();

// Real-time hubs.
builder.Services.AddScoped(sp => new RealtimeService(apiBase, sp.GetRequiredService<TokenStore>()));

await builder.Build().RunAsync();
