using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

/// <summary>
/// Maps <see cref="SaleItem"/> to the <c>SaleItems</c> table (spec §8.1 and §8.2). <see cref="SaleConfiguration"/>
/// maps the relationship: a <c>SaleId</c> foreign key, indexed.
/// </summary>
public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.OwnsOne(item => item.Product, product => ExternalIdentityMapping.Map(product, "Product"));
        builder.Navigation(item => item.Product).IsRequired();

        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(item => item.DiscountAmount).HasPrecision(18, 2);
        builder.Property(item => item.TotalAmount).HasPrecision(18, 2);
    }
}
