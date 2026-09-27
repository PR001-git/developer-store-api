using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

/// <summary>
/// Maps <see cref="Sale"/> to the <c>Sales</c> table (spec §8.1 and §8.2).
/// </summary>
public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    /// <summary>
    /// The PostgreSQL sequence that sale numbers are generated from (spec §8.3).
    /// </summary>
    public const string SaleNumberSequence = "sale_number_seq";

    /// <summary>
    /// The unique index on <c>SaleNumber</c>, over every row. <c>SaleRepository</c> recognises a violation of it by this
    /// name. It's the name the <c>AddSales</c> migration already gave the index.
    /// </summary>
    public const string SaleNumberIndex = "IX_Sales_SaleNumber";

    /// <summary>
    /// The shadow property that Npgsql maps to PostgreSQL's <c>xmin</c> system column, the concurrency token
    /// (spec decision D9). A shadow property keeps the domain free of persistence details.
    /// </summary>
    private const string RowVersion = "Version";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        // The domain creates the ids. Without ValueGeneratedNever, EF Core would treat a new item that already
        // has a key as an existing row and send an UPDATE.
        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Id).ValueGeneratedNever();

        // Unique across every row, soft-deleted ones included.
        builder.Property(sale => sale.SaleNumber).IsRequired().HasMaxLength(Sale.SaleNumberMaxLength);
        builder.HasIndex(sale => sale.SaleNumber).IsUnique().HasDatabaseName(SaleNumberIndex);

        builder.HasIndex(sale => sale.SaleDate);

        builder.OwnsOne(sale => sale.Customer, customer =>
        {
            ExternalIdentityMapping.Map(customer, "Customer");
            customer.HasIndex(identity => identity.Id);
        });
        builder.Navigation(sale => sale.Customer).IsRequired();

        builder.OwnsOne(sale => sale.Branch, branch =>
        {
            ExternalIdentityMapping.Map(branch, "Branch");
            branch.HasIndex(identity => identity.Id);
        });
        builder.Navigation(sale => sale.Branch).IsRequired();

        builder.Property(sale => sale.TotalAmount).HasPrecision(18, 2);

        builder.HasMany(sale => sale.Items).WithOne().HasForeignKey("SaleId").IsRequired();
        builder.Navigation(sale => sale.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(sale => sale.DomainEvents);

        builder.Property<uint>(RowVersion).IsRowVersion();
    }
}
