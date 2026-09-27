using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Maps the <see cref="Sale"/> aggregate to <see cref="SaleResult"/>.
/// </summary>
/// <remarks>
/// AutoMapper's flattening convention fills <c>CustomerId</c> from <c>Customer.Id</c>, <c>ProductName</c> from
/// <c>Product.Name</c>, and so on, so no member needs its own rule. The profile tests check that every member is mapped.
/// </remarks>
public sealed class SaleProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleProfile"/> class with the sale and line maps.
    /// </summary>
    public SaleProfile()
    {
        CreateMap<Sale, SaleResult>();
        CreateMap<SaleItem, SaleItemResult>();
    }
}
