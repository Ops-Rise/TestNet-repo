using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Batteries;

namespace Rise.Persistence.Batteries
{
    /// <summary>
    /// Specific configuration for <see cref="Battery"/>.
    /// </summary>
    internal class BatteryConfiguration : EntityConfiguration<Battery>
    {
        public override void Configure(EntityTypeBuilder<Battery> builder)
        {
            base.Configure(builder);

            builder.Property(b => b.Name).IsRequired().HasMaxLength(100);

            builder.Property(b => b.Status).IsRequired();

            builder
                .HasOne(b => b.User)
                .WithMany(b => b.Batteries)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
