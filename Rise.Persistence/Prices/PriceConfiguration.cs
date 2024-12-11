using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Prices;

namespace Rise.Persistence.Prices
{
    /// <summary>
    /// Specific configuration for <see cref="Price"/>.
    /// </summary>
    internal class PriceConfiguration : EntityConfiguration<Price>
    {
        public override void Configure(EntityTypeBuilder<Price> builder)
        {
            // Primary Key Configuration
            base.Configure(builder);

            // Property configuration for 'Amount'
            builder.Property(p => p.Amount).IsRequired();
        }
    }
}
