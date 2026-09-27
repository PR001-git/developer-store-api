using Ambev.DeveloperEvaluation.Application.Sales;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// Maps the Sales contracts that several endpoints share: a request line to the Application input,
/// and the Application result to the response.
/// </summary>
public sealed class SaleContractProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaleContractProfile"/> class with the shared Sales maps.
    /// </summary>
    public SaleContractProfile()
    {
        CreateMap<SaleItemRequest, SaleItemInput>();
        CreateMap<SaleResult, SaleResponse>();
        CreateMap<SaleItemResult, SaleItemResponse>();
    }
}
