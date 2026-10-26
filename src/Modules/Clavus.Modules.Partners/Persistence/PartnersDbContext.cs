using Clavus.Modules.Partners.Domain;
using Clavus.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clavus.Modules.Partners.Persistence;

/// <summary>
/// DbContext of the Partners module (schema <c>partners</c>)
/// </summary>
/// <remarks>
/// <see cref="Email"/> and <see cref="Phone"/> belong to a partner or to a contact through two optional foreign
/// keys. When an item is removed from its owner list, EF Core clears the foreign key instead of deleting the
/// row, so on save every e-mail or phone left without an owner is deleted.
/// </remarks>
/// <param name="options">Context options</param>
public sealed class PartnersDbContext(DbContextOptions<PartnersDbContext> options) : ModuleDbContext(options)
{
    /// <summary>
    /// Database schema of the module
    /// </summary>
    public const string SchemaName = "partners";

    /// <summary>
    /// Name of the shadow foreign key to the partner, in <see cref="Contact"/>, <see cref="Email"/> and <see cref="Phone"/>
    /// </summary>
    internal const string PartnerIdProperty = "PartnerId";

    /// <summary>
    /// Name of the shadow foreign key to the contact, in <see cref="Email"/> and <see cref="Phone"/>
    /// </summary>
    internal const string ContactIdProperty = "ContactId";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        DeleteOrphanContactInfo();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        DeleteOrphanContactInfo();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void DeleteOrphanContactInfo()
    {
        ChangeTracker.DetectChanges();

        var orphans = ChangeTracker.Entries()
            .Where(entry => entry.Entity is Email or Phone)
            .Where(entry => entry.State == EntityState.Modified)
            .Where(entry => entry.Property(PartnerIdProperty).CurrentValue is null
                && entry.Property(ContactIdProperty).CurrentValue is null)
            .ToList();

        foreach (var orphan in orphans)
        {
            orphan.State = EntityState.Deleted;
        }
    }
}
