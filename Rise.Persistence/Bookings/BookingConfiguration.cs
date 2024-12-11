using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;

namespace Rise.Persistence.Bookings;

/// <summary>
/// Specific configuration for <see cref="Booking"/>.
/// </summary>
class BookingConfiguration : EntityConfiguration<Booking>
{
    public override void Configure(EntityTypeBuilder<Booking> builder)
    {
        base.Configure(builder);

        // Configure the Boat relationship explicitly
        builder
            .HasOne(b => b.Boat)
            .WithMany(b => b.Bookings)
            .HasForeignKey(b => b.BoatId)
            .OnDelete(DeleteBehavior.NoAction);

        // Configure the Battery relationship explicitly
        builder
            .HasOne(b => b.Battery)
            .WithMany(b => b.Bookings)
            .HasForeignKey(b => b.BatteryId)
            .OnDelete(DeleteBehavior.NoAction);

        // Configure the User relationship explicitly
        builder
            .HasOne(b => b.User)
            .WithMany(b => b.Bookings)
            .HasForeignKey(b => b.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);

        // Other property configurations
        builder.Property(b => b.RentalDateTime).IsRequired();

        builder
            .Property(b => b.Status)
            .HasConversion<int>() // Store enum as int in the database
            .IsRequired();

        builder.Property(b => b.Remark).HasMaxLength(200);

        // Configure the Price relationship explicitly
        builder
            .HasOne(b => b.Price)
            .WithMany(b => b.Bookings)
            .HasForeignKey(b => b.PriceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
