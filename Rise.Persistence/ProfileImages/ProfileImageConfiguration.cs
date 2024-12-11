using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.ProfileImages;

namespace Rise.Persistence.ProfileImages
{
    internal class ProfileImageConfiguration : EntityConfiguration<ProfileImage>
    {
        public override void Configure(EntityTypeBuilder<ProfileImage> builder)
        {
            base.Configure(builder);

            builder.Property(p => p.UserId).IsRequired();

            builder.Property(p => p.ImageBlob)
                .IsRequired()
                .HasMaxLength(ProfileImage.MaxImageSize);

            builder.Property(p => p.ContentType)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<ProfileImage>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
