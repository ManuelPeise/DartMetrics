using Web.App.Bundles;
using Web.App.Client.Routing;
using Web.App.Components;

var builder = WebApplication.CreateBuilder(args);

ServiceRegistration.RegisterServices(builder.Services, builder.Configuration);

// The client Router includes the admin pages from this assembly when rendering on the server.
AppAreas.ServerAssembly = typeof(Program).Assembly;

var app = builder.Build();

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

app.UseAntiforgery();

app.MapStaticAssets();

app.MapBffEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Web.App.Client._Imports).Assembly);

app.Run();
