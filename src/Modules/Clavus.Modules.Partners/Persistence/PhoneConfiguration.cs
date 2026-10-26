using Clavus.Modules.Partners.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clavus.Modules.Partners.Persistence;

/// <summary>
/// Mapping of <see cref="Phone"/> to the <c>phone</c> table, owned by exactly one partner or contact
/// </summary>
internal sealed class PhoneConfiguration : IEntityTypeConfiguration<Phone>
{
    public void Configure(EntityTypeBuilder<Phone> builder)
    {
        builder.Property(phone => phone.AreaCode).HasMaxLength(Phone.AreaCodeLength);
        builder.Property(phone => phone.Number).HasMaxLength(Phone.NumberMaxLength);

        builder.ToTable(table => table.HasCheckConstraint("ck_phone_owner", "num_nonnulls(partner_id, contact_id) = 1"));
    }
}
