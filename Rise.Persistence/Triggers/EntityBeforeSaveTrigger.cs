using EntityFrameworkCore.Triggered;
using Rise.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Rise.Persistence.Triggers;

/// <summary>
/// Programmatic trigger, similar to a database trigger but database-agnostic.
/// It works across various databases (e.g., swapping from Microsoft SQL Server to MariaDB).
/// More info: https://github.com/koenbeuk/EntityFrameworkCore.Triggered
/// </summary>
public class EntityBeforeSaveTrigger : IBeforeSaveTrigger<Entity>
{
    private readonly ApplicationDbContext _dbContext;

    public EntityBeforeSaveTrigger(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void BeforeSave(ITriggerContext<Entity> context)
    {
        var entity = context.Entity;
        var currentTime = DateTime.UtcNow;

        switch (context.ChangeType)
        {
            case ChangeType.Added:
                entity.CreatedAt = currentTime;
                entity.UpdatedAt = currentTime;
                break;
            case ChangeType.Modified:
                entity.UpdatedAt = currentTime;
                break;
            case ChangeType.Deleted:
                entity.IsDeleted = true;
                entity.UpdatedAt = currentTime;
                _dbContext.Entry(entity).State = EntityState.Modified;
                break;
        }
    }
}
