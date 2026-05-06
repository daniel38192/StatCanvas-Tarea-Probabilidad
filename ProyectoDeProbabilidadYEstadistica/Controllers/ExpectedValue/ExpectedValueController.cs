using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using ProyectoDeProbabilidadYEstadistica.Models;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.ExpectedValue;

public class ExpectedValueController : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new ExpectedValueInputViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Result(ExpectedValueInputViewModel input)
    {
        var parsedValues = ParseValues(input.Values);

        if (parsedValues.Length == 0)
        {
            ModelState.AddModelError(nameof(input.Values), "Debes ingresar al menos un numero valido separado por comas.");
            return View("Index", input);
        }

        if (NormalizeFormMode(input.FormMode) == "advanced")
        {
            var parsedProbabilities = ParseValues(input.Probabilities);

            if (parsedProbabilities.Length == 0)
            {
                ModelState.AddModelError(nameof(input.Probabilities), "Debes ingresar al menos una probabilidad valida separada por comas.");
                return View("Index", input);
            }

            if (parsedValues.Length != parsedProbabilities.Length)
            {
                ModelState.AddModelError(nameof(input.Probabilities), "La cantidad de probabilidades debe coincidir con la cantidad de valores.");
                return View("Index", input);
            }

            if (parsedProbabilities.Any(probability => probability < 0d || double.IsNaN(probability) || double.IsInfinity(probability)))
            {
                ModelState.AddModelError(nameof(input.Probabilities), "Cada probabilidad debe ser un numero valido mayor o igual a 0.");
                return View("Index", input);
            }

            var probabilityTotal = parsedProbabilities.Sum();
            if (probabilityTotal > 1d)
            {
                ModelState.AddModelError(nameof(input.Probabilities), "La suma total de probabilidades no puede ser mayor que 1.");
                return View("Index", input);
            }

            var advancedExpectedValue = parsedValues.Zip(parsedProbabilities, (value, probability) => value * probability).Sum();
            return View(new ExpectedValueResultViewModel
            {
                FormMode = "advanced",
                Values = parsedValues,
                Probabilities = parsedProbabilities,
                ProbabilityTotal = probabilityTotal,
                ExpectedValue = advancedExpectedValue
            });
        }

        var probability = 1.0 / parsedValues.Length;
        var probabilities = Enumerable.Repeat(probability, parsedValues.Length).ToArray();
        var expectedValue = parsedValues.Sum(value => value * probability);

        return View(new ExpectedValueResultViewModel
        {
            FormMode = "normal",
            Values = parsedValues,
            Probabilities = probabilities,
            ProbabilityTotal = probabilities.Sum(),
            ExpectedValue = expectedValue
        });
    }

    private static double[] ParseValues(string? rawValues)
    {
        if (string.IsNullOrWhiteSpace(rawValues))
        {
            return [];
        }

        var parsedValues = new List<double>();
        var segments = rawValues.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (double.TryParse(segment, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
            {
                parsedValues.Add(value);
            }
        }

        return parsedValues.ToArray();
    }

    private static string NormalizeFormMode(string? formMode)
    {
        return formMode?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
