using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.TimeSlots;

namespace Rise.Persistence.TimeSlots
{
    /// <summary>
    /// Specific configuration for <see cref="TimeSlot"/>.
    /// </summary>
    public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
    {
        public void Configure(EntityTypeBuilder<TimeSlot> builder)
        {
            builder.ToTable("TimeSlots");

            builder.HasKey(ts => ts.Id);

            builder.Property(ts => ts.Date).IsRequired().HasColumnType("datetime2");

            builder
                .Property(ts => ts.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(ts => ts.CreatedByUserId).IsRequired();

            builder.Property(ts => ts.Reason).HasMaxLength(4000);

            builder.Property(ts => ts.Type).IsRequired().HasColumnType("int");

            builder
                .HasOne(b => b.User)
                .WithMany(b => b.TimeSlots)
                .HasForeignKey(b => b.CreatedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
