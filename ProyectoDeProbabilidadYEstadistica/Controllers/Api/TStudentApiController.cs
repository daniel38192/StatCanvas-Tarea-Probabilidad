using Microsoft.AspNetCore.Mvc;
using ProyectoDeProbabilidadYEstadistica.Models;
using ProyectoDeProbabilidadYEstadistica.Services.TStudent;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.Api;

[ApiController]
[Route("api/t-student")]
[Produces("application/json")]
public class TStudentApiController(ITStudentService tStudentService) : ControllerBase
{
    [HttpPost("calculate")]
    [Consumes("application/json")]
    public IActionResult Calculate([FromBody] TStudentCalculationRequest request)
    {
        var result = tStudentService.TryCalculate(request);

        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(ToDictionary(result.Errors)));
        }

        return Ok(TStudentApiResponse.FromResult(result.Value!));
    }

    private static Dictionary<string, string[]> ToDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }
}
