using AgriCheck.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly AgriCheckDbContext _dbContext;

    public HealthController(AgriCheckDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        var response = new
        {
            success = canConnect,
            data = new
            {
                status = canConnect ? "healthy" : "unhealthy",
                service = "AgriCheck V3 API",
                version = "0.1.0",
                database = canConnect ? "connected" : "disconnected",
                timestamp = DateTime.UtcNow
            },
            errors = canConnect ? null : new[] { new { code = "DB_UNREACHABLE", message = "Database connection failed." } }
        };

        return canConnect ? Ok(response) : StatusCode(503, response);
    }
}
