using Microsoft.AspNetCore.Mvc;
using ProyectoDeProbabilidadYEstadistica.Models;
using ProyectoDeProbabilidadYEstadistica.Services.TStudent;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.TStudent;

public class TStudentController(ITStudentService tStudentService) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = new TStudentInputViewModel();
        PopulateHypothesisTexts(model, model.HypothesizedMean ?? 0d, model.TestType);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Calculate(TStudentInputViewModel input)
    {
        var parsedValuesResult = tStudentService.TryParseValues(input.RawValues);

        if (!parsedValuesResult.Succeeded)
        {
            AddErrorsToModelState(parsedValuesResult.Errors);
            PopulateHypothesisTexts(input, input.HypothesizedMean ?? 0d, input.TestType);
            return View("Index", input);
        }

        var calculationResult = tStudentService.TryCalculate(new TStudentCalculationRequest
        {
            Values = parsedValuesResult.Value,
            HypothesizedMean = input.HypothesizedMean,
            Alpha = input.Alpha,
            TestType = input.TestType
        });

        if (!calculationResult.Succeeded)
        {
            AddErrorsToModelState(calculationResult.Errors);
            PopulateHypothesisTexts(input, input.HypothesizedMean ?? 0d, input.TestType);
            return View("Index", input);
        }

        return View("Result", calculationResult.Value);
    }

    [HttpGet]
    public IActionResult Svg([FromQuery] int degreesOfFreedom, [FromQuery] double tStatistic, [FromQuery] double criticalValue, [FromQuery] string testType = "two-tailed")
    {
        var result = tStudentService.TryBuildSvg(degreesOfFreedom, tStatistic, criticalValue, testType);
        return result.Succeeded
            ? Content(result.Value!, "image/svg+xml")
            : BadRequest("Invalid parameters.");
    }

    private void PopulateHypothesisTexts(TStudentInputViewModel input, double hypothesizedMean, string testType)
    {
        var hypotheses = tStudentService.BuildHypothesisTexts(hypothesizedMean, testType);
        input.NullHypothesisText = hypotheses.NullHypothesisText;
        input.AlternativeHypothesisText = hypotheses.AlternativeHypothesisText;
    }

    private void AddErrorsToModelState(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        foreach (var pair in errors)
        {
            foreach (var message in pair.Value)
            {
                ModelState.AddModelError(pair.Key, message);
            }
        }
    }
}
