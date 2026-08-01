using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tripwalaah.LocationService.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        return report.Status == HealthStatus.Healthy
            ? Ok(new { status = "Healthy" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = report.Status.ToString() });
    }
}
