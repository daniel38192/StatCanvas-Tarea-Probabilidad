using Microsoft.AspNetCore.Mvc;
using ProyectoDeProbabilidadYEstadistica.Models;
using ProyectoDeProbabilidadYEstadistica.Services.StandardNormal;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.StandardNormal;

public class StandardNormalController(IStandardNormalService standardNormalService) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new StandardNormalInputViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Calculate(StandardNormalInputViewModel input)
    {
        var request = new StandardNormalCalculationRequest
        {
            FormMode = input.FormMode,
            CalculationMode = input.CalculationMode,
            ZValue = input.ZValue,
            AreaValue = input.AreaValue,
            AreaSide = input.AreaSide,
            IntervalRange = $"[{input.LowerBound?.Trim()}, {input.UpperBound?.Trim()}]"
        };
        var result = standardNormalService.TryCalculate(request);

        if (!result.Succeeded)
        {
            AddErrorsToModelState(result.Errors);
            return View("Index", input);
        }

        return View("Result", result.Value);
    }

    [HttpGet]
    public IActionResult Svg([FromQuery] double? zValue, [FromQuery] string areaSide = "left", [FromQuery] string? intervalRange = null)
    {
        CalculationResult<string> result;

        if (!string.IsNullOrWhiteSpace(intervalRange))
        {
            result = standardNormalService.TryBuildRangeSvg(intervalRange);
        }
        else if (zValue.HasValue)
        {
            result = standardNormalService.TryBuildSvg(zValue.Value, areaSide);
        }
        else
        {
            return BadRequest("Invalid parameters.");
        }

        return result.Succeeded
            ? Content(result.Value!, "image/svg+xml")
            : BadRequest("Invalid parameters.");
    }

    private void AddErrorsToModelState(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        foreach (var pair in errors)
        {
            var modelKeys = pair.Key == "intervalRange"
                ? new[] { nameof(StandardNormalInputViewModel.LowerBound), nameof(StandardNormalInputViewModel.UpperBound) }
                : new[] { pair.Key };

            foreach (var message in pair.Value)
            {
                foreach (var key in modelKeys)
                {
                    ModelState.AddModelError(key, message);
                }
            }
        }
    }
}
