using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

/// <summary>
/// Maps the list request to its query. The sales use the map in <see cref="SaleContractProfile"/>.
/// </summary>
public sealed class ListSalesProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSalesProfile"/> class with the request map.
    /// </summary>
    public ListSalesProfile()
    {
        CreateMap<ListSalesRequest, ListSalesQuery>();
    }
}
