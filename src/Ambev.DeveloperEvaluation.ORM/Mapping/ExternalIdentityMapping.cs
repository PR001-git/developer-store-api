using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

/// <summary>
/// Maps an <see cref="ExternalIdentity"/> as an owned type into two columns of its owner's table
/// (spec decision D2): an id and a copied name, with no foreign key.
/// </summary>
internal static class ExternalIdentityMapping
{
    /// <summary>
    /// Maps the identity to the columns <c>{prefix}Id</c> (<c>uuid</c>) and <c>{prefix}Name</c> (<c>varchar(100)</c>).
    /// </summary>
    /// <typeparam name="TOwner">The entity that owns the identity.</typeparam>
    /// <param name="identity">The owned navigation to configure.</param>
    /// <param name="prefix">The column prefix, such as <c>Customer</c>.</param>
    public static void Map<TOwner>(OwnedNavigationBuilder<TOwner, ExternalIdentity> identity, string prefix)
        where TOwner : class
    {
        identity.Property(value => value.Id).HasColumnName($"{prefix}Id");
        identity.Property(value => value.Name)
            .HasColumnName($"{prefix}Name")
            .IsRequired()
            .HasMaxLength(ExternalIdentity.NameMaxLength);
    }
}
