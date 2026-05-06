using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.Variance;

public class VarianceController: Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // POST /Variance/CalculateVariance
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CalculateVariance([FromForm] string values, [FromForm] bool isPopulation)
    {
        ViewBag.Values = values;
        ViewBag.IsPopulation = isPopulation;

        var parsedValues = ParseValues(values);

        if (parsedValues.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Debes ingresar al menos un numero valido separado por comas.");
            return View("Index");
        }

        if (!isPopulation && parsedValues.Length < 2)
        {
            ModelState.AddModelError(string.Empty, "La varianza muestral requiere al menos dos numeros.");
            return View("Index");
        }

        var result = isPopulation ? new
            {
                Type = "Población", 
                Variance = PopulationVariance(parsedValues), 
                Mean = parsedValues.Average(),
                Std = Math.Sqrt(PopulationVariance(parsedValues))
            } : 
            new
            {
                Type = "Muestra", 
                Variance = SampleVariance(parsedValues), 
                Mean = parsedValues.Average(),
                Std = Math.Sqrt(SampleVariance(parsedValues))
            };
        
        return View("Result", result);
    }

    private double SampleVariance(double[] values)
    {
        var average = values.Average();
        var sumOfSquares = values.Sum(value => Math.Pow(value - average, 2));
        return sumOfSquares / (values.Length - 1);
    }

    private double PopulationVariance(double[] values)
    {
        var average = values.Average();
        var sumOfSquares = values.Sum(value => Math.Pow(value - average, 2));
        return sumOfSquares / values.Length;
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
}
