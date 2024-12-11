using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Boats;

namespace Rise.Persistence.Boats
{
    /// <summary>
    /// Specific configuration for <see cref="Boat"/>.
    /// </summary>
    internal class BoatConfiguration : EntityConfiguration<Boat>
    {
        public override void Configure(EntityTypeBuilder<Boat> builder)
        {
            // Primary Key Configuration
            base.Configure(builder);

            // Property configuration for 'Name'
            builder.Property(b => b.Name).IsRequired().HasMaxLength(255);

            // Property configuration for 'Status' (Enum)
            builder
                .Property(b => b.Status)
                .HasConversion<int>() // Store the enum as an int in the database
                .IsRequired();
        }
    }
}
