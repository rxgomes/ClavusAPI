using Clavus.Modules.Partners.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clavus.Modules.Partners.Persistence;

/// <summary>
/// Mapping of <see cref="Contact"/> to the <c>contact</c> table
/// </summary>
internal sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.Property(contact => contact.Name).HasMaxLength(Contact.NameMaxLength);
        builder.Property(contact => contact.Role).HasMaxLength(Contact.RoleMaxLength);

        builder.HasMany(contact => contact.Emails)
            .WithOne()
            .HasForeignKey(PartnersDbContext.ContactIdProperty)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(contact => contact.Phones)
            .WithOne()
            .HasForeignKey(PartnersDbContext.ContactIdProperty)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
