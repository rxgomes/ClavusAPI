using Clavus.Modules.Partners.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clavus.Modules.Partners.Persistence;

/// <summary>
/// Mapping of <see cref="Email"/> to the <c>email</c> table, owned by exactly one partner or contact
/// </summary>
internal sealed class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.Property(email => email.Address).HasMaxLength(Email.AddressMaxLength);
        builder.Property(email => email.Type).HasMaxLength(Email.TypeMaxLength);

        builder.ToTable(table => table.HasCheckConstraint("ck_email_owner", "num_nonnulls(partner_id, contact_id) = 1"));
    }
}
