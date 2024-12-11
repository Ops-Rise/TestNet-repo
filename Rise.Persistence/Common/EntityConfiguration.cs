using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Common;

namespace Rise.Persistence.Common;

/// <summary>
/// Base configuration for an <see cref="Entity"/>
/// </summary>
/// <typeparam name="TEntity"></typeparam>
internal class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : Entity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // All tables are singlular named and have the name of the class e.g. Product instead of Products.
        builder.ToTable(typeof(TEntity).Name);

        // CreatedAt should be filled in by the database when using raw SQL.
        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        // UpdatedAt should be filled in by the database when using raw SQL.
        builder.Property(e => e.UpdatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsDeleted)
               .HasDefaultValue(false);
    }
}

