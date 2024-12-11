using Bogus;
using Rise.Domain.Common;

namespace Rise.Fakers.Common;

/// <summary>
///     Base clase to create <see cref="Entity" /> fakers.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class EntityFaker<TEntity> : Faker<TEntity> where TEntity : Entity
{
    /// <summary>
    ///     Default constructor to generate an Id and set locale to 'NL' as default.
    /// </summary>
    /// <param name="locale"></param>
    protected EntityFaker(string locale = "nl") : base(locale)
    {
        var id = 1;
        // ReSharper disable once VirtualMemberCallInConstructor
        RuleFor(x => x.Id, _ => id++);
        // ReSharper disable once VirtualMemberCallInConstructor
        //RuleFor(x => x.Id, _ => default);
    }

    /// <summary>
    ///     Builder method to reset the Id as the default so the (relational) database can generate one.
    /// </summary>
    /// <returns></returns>
    public EntityFaker<TEntity> AsTransient()
    {
        RuleFor(x => x.Id, _ => default);
        return this;
    }
}