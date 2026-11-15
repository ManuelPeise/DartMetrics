using Microsoft.AspNetCore.HttpOverrides;
using Web.App.Bundles;
using Web.App.Client.Routing;
using Web.App.Components;

var builder = WebApplication.CreateBuilder(args);

ServiceRegistration.RegisterServices(builder.Services, builder.Configuration);

// The client Router includes the admin pages from this assembly when rendering on the server.
AppAreas.ServerAssembly = typeof(Program).Assembly;

var app = builder.Build();

// Behind a reverse proxy / tunnel (Raspberry Pi deployment): take scheme and client IP from the proxy,
// so the __Host- cookie (Secure) and the per-IP rate limit keep working. Off by default.
if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    // The proxy runs in the same Docker network; its address is not known in advance.
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapBffEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Web.App.Client._Imports).Assembly);

app.Run();
