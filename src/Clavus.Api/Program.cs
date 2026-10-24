using Clavus.Api.Cors;
using Clavus.Api.Errors;
using Clavus.Api.HealthChecks;
using Clavus.Api.Modules;
using Clavus.Shared.Abstractions;
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

builder.Services.AddClavusErrorHandling();
builder.Services.AddClavusCors(builder.Configuration);
builder.Services.AddClavusDatabase(builder.Configuration);
builder.Services.AddClavusHealthChecks();

foreach (var module in ModuleCatalog.All)
{
    builder.Services.AddSingleton(module);
    module.RegisterServices(builder.Services, builder.Configuration);
}

var app = builder.Build();

app.UseClavusErrorHandling();
app.UseClavusCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapClavusHealthChecks();

// Endpoints vêm dos módulos registrados no container (ModuleCatalog; nos testes, também módulos de teste).
foreach (var module in app.Services.GetServices<IModule>())
{
    module.MapEndpoints(app);
}

app.Run();

/// <summary>
/// Exposto para os testes de integração (<c>WebApplicationFactory&lt;Program&gt;</c>).
/// </summary>
public partial class Program;
