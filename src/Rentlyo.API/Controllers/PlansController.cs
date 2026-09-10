using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Tenant;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/plans")]
public class PlansController(ITenantService tenantService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlanResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var data = await tenantService.ListPlansAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PlanResponse>>.Success(data));
    }
}
