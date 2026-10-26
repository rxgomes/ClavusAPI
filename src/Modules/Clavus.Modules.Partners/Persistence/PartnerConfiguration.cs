using Clavus.Modules.Partners.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clavus.Modules.Partners.Persistence;

/// <summary>
/// Mapping of <see cref="Partner"/> to the <c>partner</c> table
/// </summary>
internal sealed class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    private const int DocumentMaxLength = 14;

    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.Property(partner => partner.Name).HasMaxLength(Partner.NameMaxLength);
        builder.Property(partner => partner.TradeName).HasMaxLength(Partner.NameMaxLength);
        builder.Property(partner => partner.MunicipalRegistration).HasMaxLength(Partner.RegistrationMaxLength);
        builder.Property(partner => partner.StateRegistration).HasMaxLength(Partner.RegistrationMaxLength);
        builder.Property(partner => partner.IsClient).HasDefaultValue(false);
        builder.Property(partner => partner.IsSupplier).HasDefaultValue(false);

        builder.Property(partner => partner.Document)
            .HasConversion(document => document.Value, value => new Document(value))
            .HasMaxLength(DocumentMaxLength);
        builder.HasIndex(partner => partner.Document).IsUnique();

        builder.ComplexProperty(partner => partner.Address, address =>
        {
            address.Property(a => a.ZipCode).HasMaxLength(Address.ZipCodeLength);
            address.Property(a => a.StreetType).HasMaxLength(Address.StreetTypeMaxLength);
            address.Property(a => a.Street).HasMaxLength(Address.NameMaxLength);
            address.Property(a => a.Number).HasMaxLength(Address.NumberMaxLength);
            address.Property(a => a.Neighborhood).HasMaxLength(Address.NameMaxLength);
            address.Property(a => a.City).HasMaxLength(Address.NameMaxLength);
            address.Property(a => a.State).HasMaxLength(Address.StateMaxLength);
            address.Property(a => a.StateCode).HasMaxLength(Address.StateCodeLength);
        });

        builder.HasMany(partner => partner.Contacts)
            .WithOne()
            .HasForeignKey(PartnersDbContext.PartnerIdProperty)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(partner => partner.Emails)
            .WithOne()
            .HasForeignKey(PartnersDbContext.PartnerIdProperty)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(partner => partner.Phones)
            .WithOne()
            .HasForeignKey(PartnersDbContext.PartnerIdProperty)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
