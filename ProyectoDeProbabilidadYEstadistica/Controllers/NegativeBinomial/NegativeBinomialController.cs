using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ProyectoDeProbabilidadYEstadistica.Models;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.NegativeBinomial;

public class NegativeBinomialController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new NegativeBinomialInputViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Calculate(NegativeBinomialInputViewModel input)
    {
        var errors = new Dictionary<string, List<string>>();
        var mode = NormalizeCalculationMode(input.CalculationMode);

        if (!input.TrialSize.HasValue || input.TrialSize <= 0)
        {
            AddError(errors, nameof(input.TrialSize), "Ingresa un tamano de ensayos valido mayor que 0.");
        }

        if (!input.SuccessProbability.HasValue ||
            double.IsNaN(input.SuccessProbability.Value) ||
            double.IsInfinity(input.SuccessProbability.Value))
        {
            AddError(errors, nameof(input.SuccessProbability), "Ingresa una probabilidad de exito valida.");
        }
        else if (input.SuccessProbability <= 0d || input.SuccessProbability >= 1d)
        {
            AddError(errors, nameof(input.SuccessProbability), "La probabilidad de exito debe ser mayor que 0 y menor que 1.");
        }

        if (mode is not ("single" or "interval" or "sum"))
        {
            AddError(errors, nameof(input.CalculationMode), "Selecciona un modo de calculo valido.");
        }

        if (mode == "single")
        {
            ValidateNonNegativeInteger(input.XValue, nameof(input.XValue), "Ingresa un valor x valido.", errors);
        }
        else if (mode == "interval")
        {
            ValidateNonNegativeInteger(input.IntervalStart, nameof(input.IntervalStart), "Ingresa un limite inicial valido.", errors);
            ValidateNonNegativeInteger(input.IntervalEnd, nameof(input.IntervalEnd), "Ingresa un limite final valido.", errors);

            if (input.IntervalStart.HasValue && input.IntervalEnd.HasValue && input.IntervalStart > input.IntervalEnd)
            {
                AddError(errors, nameof(input.IntervalEnd), "El limite final debe ser mayor o igual al limite inicial.");
            }
        }
        else if (mode == "sum")
        {
            ValidateNonNegativeInteger(input.SumStart, nameof(input.SumStart), "Ingresa un limite inferior valido para la suma.", errors);
            ValidateNonNegativeInteger(input.SumEnd, nameof(input.SumEnd), "Ingresa un limite superior valido para la suma.", errors);

            if (input.SumStart.HasValue && input.SumEnd.HasValue && input.SumStart > input.SumEnd)
            {
                AddError(errors, nameof(input.SumEnd), "El limite superior de la suma debe ser mayor o igual al inferior.");
            }
        }

        if (errors.Count > 0)
        {
            AddErrorsToModelState(errors);
            return View("Index", input);
        }

        var r = input.TrialSize!.Value;
        var p = input.SuccessProbability!.Value;
        var q = 1d - p;

        NegativeBinomialResultViewModel result = mode switch
        {
            "single" => BuildSingleResult(r, p, q, input.XValue!.Value),
            "interval" => BuildRangeResult(r, p, q, input.IntervalStart!.Value, input.IntervalEnd!.Value, "interval"),
            _ => BuildRangeResult(r, p, q, input.SumStart!.Value, input.SumEnd!.Value, "sum")
        };

        return View("Result", result);
    }

    private static NegativeBinomialResultViewModel BuildSingleResult(int r, double p, double q, int x)
    {
        return new NegativeBinomialResultViewModel
        {
            TrialSize = r,
            SuccessProbability = p,
            FailureProbability = q,
            CalculationMode = "single",
            XValue = x,
            OperationLabel = $"P(X = {x})",
            Probability = NegativeBinomialProbability(r, x, p, q),
            Terms =
            [
                new NegativeBinomialTermViewModel
                {
                    X = x,
                    Probability = NegativeBinomialProbability(r, x, p, q)
                }
            ]
        };
    }

    private static NegativeBinomialResultViewModel BuildRangeResult(int r, double p, double q, int startX, int endX, string mode)
    {
        var terms = Enumerable.Range(startX, endX - startX + 1)
            .Select(x => new NegativeBinomialTermViewModel
            {
                X = x,
                Probability = NegativeBinomialProbability(r, x, p, q)
            })
            .ToArray();

        var operationLabel = mode == "interval"
            ? $"P({startX} <= X <= {endX})"
            : $"Σ P(X = x), x = {startX}..{endX}";

        return new NegativeBinomialResultViewModel
        {
            TrialSize = r,
            SuccessProbability = p,
            FailureProbability = q,
            CalculationMode = mode,
            StartX = startX,
            EndX = endX,
            OperationLabel = operationLabel,
            Probability = terms.Sum(term => term.Probability),
            Terms = terms
        };
    }

    private static double NegativeBinomialProbability(int r, int x, double p, double q)
    {
        return Combination(x + r - 1, x) * Math.Pow(p, r) * Math.Pow(q, x);
    }

    private static double Combination(int n, int k)
    {
        if (k < 0 || k > n)
        {
            return 0d;
        }

        k = Math.Min(k, n - k);
        var result = 1d;

        for (var i = 1; i <= k; i++)
        {
            result *= (n - k + i) / (double)i;
        }

        return result;
    }

    private static void ValidateNonNegativeInteger(int? value, string key, string message, Dictionary<string, List<string>> errors)
    {
        if (!value.HasValue || value < 0)
        {
            AddError(errors, key, message);
        }
    }

    private static string NormalizeCalculationMode(string? mode)
    {
        return mode?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private void AddErrorsToModelState(IReadOnlyDictionary<string, List<string>> errors)
    {
        foreach (var pair in errors)
        {
            foreach (var message in pair.Value)
            {
                ModelState.AddModelError(pair.Key, message);
            }
        }
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
