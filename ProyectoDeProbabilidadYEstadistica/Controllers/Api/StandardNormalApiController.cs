using Microsoft.AspNetCore.Mvc;
using ProyectoDeProbabilidadYEstadistica.Models;
using ProyectoDeProbabilidadYEstadistica.Services.StandardNormal;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.Api;

[ApiController]
[Route("api/standard-normal")]
[Produces("application/json")]
public class StandardNormalApiController(IStandardNormalService standardNormalService) : ControllerBase
{
    [HttpPost("calculate")]
    [Consumes("application/json")]
    public IActionResult Calculate([FromBody] StandardNormalCalculationRequest request)
    {
        var result = standardNormalService.TryCalculate(request);

        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(ToDictionary(result.Errors)));
        }

        return Ok(StandardNormalApiResponse.FromResult(result.Value!));
    }

    private static Dictionary<string, string[]> ToDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }
}
