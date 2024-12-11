using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Addresses;

namespace Rise.Persistence.Addresses;

/// <summary>
/// Specific configuration for <see cref="Address"/>.
/// </summary>
internal class UserConfiguration : EntityConfiguration<Address>
{
    public override void Configure(EntityTypeBuilder<Address> builder)
    {
        base.Configure(builder);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();
        builder.Property(a => a.Street)
            .IsRequired().HasMaxLength(100);

        builder.Property(a => a.HouseNumber)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(a => a.UnitNumber).HasMaxLength(10);

        builder.Property(a => a.City)
            .IsRequired().HasMaxLength(50);

        builder.Property(a => a.PostalCode).IsRequired().HasMaxLength(10);
    }
}