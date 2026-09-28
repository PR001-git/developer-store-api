using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// The Sales API. Every endpoint needs a JWT from <c>POST /api/auth</c>; no role is checked (spec decision D13).
/// </summary>
[ApiController]
[Route("api/sales")]
[Authorize]
public sealed class SalesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalesController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator that runs the Sales use cases.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    public SalesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a sale. The domain computes the discounts and totals.
    /// </summary>
    /// <param name="request">The sale to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 with the created sale, and its address in the <c>Location</c> header.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(_mapper.Map<CreateSaleCommand>(request), cancellationToken);

        return Created(nameof(GetSale), new { id = result.Id }, _mapper.Map<SaleResponse>(result), "Sale created successfully");
    }

    /// <summary>
    /// Gets one sale, with its items.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the sale.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleQuery(id), cancellationToken);

        return Ok(_mapper.Map<SaleResponse>(result), "Sale retrieved successfully");
    }

    /// <summary>
    /// Cancels a sale. Its items and total stay as they were; a cancelled sale can't be changed again.
    /// </summary>
    /// <param name="id">The id of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with the cancelled sale.</returns>
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleCommand(id), cancellationToken);

        return Ok(_mapper.Map<SaleResponse>(result), "Sale cancelled successfully");
    }
}
