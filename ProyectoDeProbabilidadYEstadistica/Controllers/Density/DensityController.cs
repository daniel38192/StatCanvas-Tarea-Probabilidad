using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ProyectoDeProbabilidadYEstadistica.Models;

namespace ProyectoDeProbabilidadYEstadistica.Controllers.Density;

public class DensityController : Controller
{
    private const int ChartWidth = 900;
    private const int ChartHeight = 420;
    private const int SampleCount = 180;

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Generate([FromForm] string values)
    {
        ViewBag.Values = values;
        var normalizedValues = NormalizeValues(ParseValues(values));
        if (normalizedValues.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Debes ingresar al menos un numero valido.");
            return View("Index");
        }

        var result = BuildResult(normalizedValues);
        ViewBag.Values = string.Join(", ", normalizedValues.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));
        return View("Result", result);
    }

    [HttpGet]
    public IActionResult Svg([FromQuery] string values)
    {
        var normalizedValues = NormalizeValues(ParseValues(values));
        if (normalizedValues.Length == 0)
        {
            return BadRequest("Provide at least one valid numeric value.");
        }

        var result = BuildResult(normalizedValues);
        return Content(result.Svg, "image/svg+xml", Encoding.UTF8);
    }

    private static double[] NormalizeValues(IEnumerable<double> values)
    {
        return values.Where(value => !double.IsNaN(value) && !double.IsInfinity(value)).ToArray();
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

    private static DensityResultViewModel BuildResult(IReadOnlyList<double> values)
    {
        var mean = values.Average();
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Count;
        var standardDeviation = Math.Sqrt(variance);
        var bandwidth = CalculateBandwidth(values, standardDeviation);
        var points = BuildDensityPoints(values, bandwidth, SampleCount);
        var svg = BuildSvg(points, values);

        return new DensityResultViewModel
        {
            Values = values.ToArray(),
            Points = points,
            Mean = mean,
            StandardDeviation = standardDeviation,
            Bandwidth = bandwidth,
            Svg = svg
        };
    }

    private static double CalculateBandwidth(IReadOnlyList<double> values, double standardDeviation)
    {
        if (values.Count == 1)
        {
            return 1d;
        }

        var min = values.Min();
        var max = values.Max();
        var range = max - min;
        var silverman = 1.06d * Math.Max(standardDeviation, 1e-6d) * Math.Pow(values.Count, -0.2d);
        var fallback = range > 0d ? range / 12d : 1d;

        return Math.Max(silverman, fallback);
    }

    private static List<DensityPointViewModel> BuildDensityPoints(IReadOnlyList<double> values, double bandwidth, int sampleCount)
    {
        var min = values.Min() - (3d * bandwidth);
        var max = values.Max() + (3d * bandwidth);

        if (Math.Abs(max - min) < 1e-9d)
        {
            min -= 1d;
            max += 1d;
        }

        var step = (max - min) / (sampleCount - 1);
        var points = new List<DensityPointViewModel>(sampleCount);

        for (var index = 0; index < sampleCount; index++)
        {
            var x = min + (step * index);
            var density = values.Sum(value => GaussianKernel((x - value) / bandwidth)) / (values.Count * bandwidth);
            points.Add(new DensityPointViewModel { X = x, Y = density });
        }

        return points;
    }

    private static double GaussianKernel(double value)
    {
        return Math.Exp(-0.5d * value * value) / Math.Sqrt(2d * Math.PI);
    }

    private static string BuildSvg(IReadOnlyList<DensityPointViewModel> points, IReadOnlyList<double> values)
    {
        const int left = 64;
        const int right = 24;
        const int top = 24;
        const int bottom = 48;

        var plotWidth = ChartWidth - left - right;
        var plotHeight = ChartHeight - top - bottom;
        var xMin = points.Min(point => point.X);
        var xMax = points.Max(point => point.X);
        var yMax = Math.Max(points.Max(point => point.Y), 1e-6d);

        double ScaleX(double x) => left + ((x - xMin) / (xMax - xMin) * plotWidth);
        double ScaleY(double y) => top + (plotHeight - ((y / yMax) * plotHeight));

        var polyline = string.Join(" ",
            points.Select(point => $"{ScaleX(point.X).ToString("0.##", CultureInfo.InvariantCulture)},{ScaleY(point.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));

        var markers = new StringBuilder();
        foreach (var value in values)
        {
            var x = ScaleX(value).ToString("0.##", CultureInfo.InvariantCulture);
            markers.AppendLine($"<line x1=\"{x}\" y1=\"{top}\" x2=\"{x}\" y2=\"{top + plotHeight}\" stroke=\"#d97706\" stroke-opacity=\"0.28\" stroke-width=\"1\" />");
        }

        var xAxisLabels = BuildXAxisLabels(xMin, xMax, plotWidth, left, top + plotHeight);
        var yAxisLabels = BuildYAxisLabels(yMax, plotHeight, left, top);

        return $"""
<svg xmlns="http://www.w3.org/2000/svg" width="{ChartWidth}" height="{ChartHeight}" viewBox="0 0 {ChartWidth} {ChartHeight}" role="img" aria-label="Probability density chart">
  <rect width="100%" height="100%" fill="#fffdf7" rx="18" />
  <rect x="{left}" y="{top}" width="{plotWidth}" height="{plotHeight}" fill="#fff" stroke="#e5e7eb" />
  <line x1="{left}" y1="{top + plotHeight}" x2="{left + plotWidth}" y2="{top + plotHeight}" stroke="#111827" stroke-width="1.4" />
  <line x1="{left}" y1="{top}" x2="{left}" y2="{top + plotHeight}" stroke="#111827" stroke-width="1.4" />
  {markers}
  <polyline fill="none" stroke="#0f766e" stroke-width="3" points="{polyline}" />
  <text x="{left + (plotWidth / 2)}" y="{ChartHeight - 10}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="14" fill="#374151">Value</text>
  <text x="18" y="{top + (plotHeight / 2)}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="14" fill="#374151" transform="rotate(-90 18 {top + (plotHeight / 2)})">Density</text>
  {xAxisLabels}
  {yAxisLabels}
</svg>
""";
    }

    private static string BuildXAxisLabels(double xMin, double xMax, int plotWidth, int left, int baselineY)
    {
        var builder = new StringBuilder();
        const int steps = 5;

        for (var i = 0; i <= steps; i++)
        {
            var ratio = i / (double)steps;
            var x = left + (ratio * plotWidth);
            var value = xMin + ((xMax - xMin) * ratio);
            builder.AppendLine(
                $"<text x=\"{x.ToString("0.##", CultureInfo.InvariantCulture)}\" y=\"{(baselineY + 20).ToString(CultureInfo.InvariantCulture)}\" text-anchor=\"middle\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"12\" fill=\"#4b5563\">{value.ToString("0.##", CultureInfo.InvariantCulture)}</text>");
        }

        return builder.ToString();
    }

    private static string BuildYAxisLabels(double yMax, int plotHeight, int left, int top)
    {
        var builder = new StringBuilder();
        const int steps = 4;

        for (var i = 0; i <= steps; i++)
        {
            var ratio = i / (double)steps;
            var y = top + plotHeight - (ratio * plotHeight);
            var value = yMax * ratio;
            builder.AppendLine(
                $"<text x=\"{(left - 10).ToString(CultureInfo.InvariantCulture)}\" y=\"{(y + 4).ToString("0.##", CultureInfo.InvariantCulture)}\" text-anchor=\"end\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"12\" fill=\"#4b5563\">{value.ToString("0.###", CultureInfo.InvariantCulture)}</text>");
        }

        return builder.ToString();
    }
}
