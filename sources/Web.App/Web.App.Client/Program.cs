using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web.App.Client.Authentication;
using Web.App.Client.Services;
using Web.App.Client.Services.Api;
using Web.App.Client.Services.Remote;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddTransient<CsrfHeaderHandler>();
builder.Services
    .AddHttpClient(BffHttpClient.Name, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<CsrfHeaderHandler>();

builder.Services.AddScoped<BffAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<BffAuthenticationStateProvider>());

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ApiClient>();

// Typisierte API-Clients (Seiten kennen keine URLs)
builder.Services.AddScoped<IProfileApi, ProfileApi>();
builder.Services.AddScoped<PlayerProfileState>();
builder.Services.AddScoped<IBotGameApi, BotGameApi>();
builder.Services.AddScoped<ITournamentApi, TournamentApi>();
builder.Services.AddScoped<ITrainingApi, TrainingApi>();
builder.Services.AddScoped<IStatisticsApi, StatisticsApi>();
builder.Services.AddScoped<IProgressApi, ProgressApi>();

// Handy als Eingabegerät (Hub /hubs/remote)
builder.Services.AddScoped<RemoteInputConnection>();

await builder.Build().RunAsync();
