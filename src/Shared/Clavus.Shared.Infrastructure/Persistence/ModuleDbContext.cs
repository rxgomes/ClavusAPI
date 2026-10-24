using Microsoft.EntityFrameworkCore;

namespace Clavus.Shared.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for business modules. Places every table in the module schema and maps
/// <see cref="DateTime"/> to <c>timestamp without time zone</c> by convention.
/// </summary>
/// <remarks>
/// <para>
/// Register the concrete context with
/// <see cref="DatabaseServiceCollectionExtensions.AddModuleDbContext{TContext}"/>, using the same
/// schema returned by <see cref="Schema"/>.
/// </para>
/// <para>
/// Columns <c>timestamp without time zone</c> do not accept <see cref="DateTime"/> values with
/// <see cref="DateTimeKind.Utc"/>: Npgsql throws on write. Use <see cref="DateTimeKind.Unspecified"/>
/// or <see cref="DateTimeKind.Local"/> (for example <see cref="DateTime.Now"/>).
/// </para>
/// </remarks>
/// <param name="options">Context options</param>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Database schema owned by the module, in lowercase snake_case (for example <c>vendas</c>)
    /// </summary>
    protected abstract string Schema { get; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");

        base.ConfigureConventions(configurationBuilder);
    }
}
