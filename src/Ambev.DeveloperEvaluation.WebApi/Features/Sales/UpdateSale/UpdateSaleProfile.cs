using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

/// <summary>
/// Maps the update-sale request to its command. The id comes from the route, so the controller sets it. The lines use
/// the map in <see cref="SaleContractProfile"/>.
/// </summary>
public sealed class UpdateSaleProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSaleProfile"/> class with the request map.
    /// </summary>
    public UpdateSaleProfile()
    {
        CreateMap<UpdateSaleRequest, UpdateSaleCommand>()
            .ForMember(command => command.Id, options => options.Ignore());
    }
}
