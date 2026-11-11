using Web.Core.Bundles;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddServices(configuration);

var app = builder.Build();

app.ConfigureApp(configuration);

app.Run();
