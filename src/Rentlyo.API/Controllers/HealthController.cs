using Microsoft.AspNetCore.Mvc;
using Rentlyo.Shared.Constants;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var data = new
        {
            status = "Healthy",
            service = AppConstants.AppName
        };

        return Ok(ApiResponse<object>.Success(data));
    }
}
