using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Users;

namespace Rise.Persistence.Users;

/// <summary>
/// Specific configuration for <see cref="User"/>.
/// </summary>
internal class UserConfiguration : EntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.Property(u => u.Email).IsRequired();

        builder.Property(u => u.Firstname).IsRequired(false).HasMaxLength(50);

        builder.Property(u => u.Lastname).IsRequired(false).HasMaxLength(50);

        builder.Property(u => u.BirthDay).IsRequired(false);

        builder.Property(u => u.PhoneNumber).IsRequired(false).HasMaxLength(15);

        builder.Property(u => u.IsRegistrationComplete).IsRequired().HasDefaultValue(false);

        builder
            .HasOne(u => u.Address)
            .WithOne()
            .IsRequired(false)
            .HasForeignKey<User>(u => u.AddressId)
            .OnDelete(DeleteBehavior.NoAction);

        //password and roles on auth0 database
    }
}
