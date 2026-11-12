using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web.App.Client.Authentication;
using Web.App.Client.Services;

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

await builder.Build().RunAsync();
