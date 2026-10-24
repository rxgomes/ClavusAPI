using Clavus.Api.HealthChecks;
using Clavus.Api.Modules;
using Clavus.Shared.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Clavus API";
        return Task.CompletedTask;
    });
});

builder.Services.AddClavusDatabase(builder.Configuration);
builder.Services.AddClavusHealthChecks();

foreach (var module in ModuleCatalog.All)
{
    module.RegisterServices(builder.Services, builder.Configuration);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapClavusHealthChecks();

foreach (var module in ModuleCatalog.All)
{
    module.MapEndpoints(app);
}

app.Run();

/// <summary>
/// Exposto para os testes de integração (<c>WebApplicationFactory&lt;Program&gt;</c>).
/// </summary>
public partial class Program;
