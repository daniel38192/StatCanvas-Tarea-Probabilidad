using System.Globalization;
using System.Text;
using ProyectoDeProbabilidadYEstadistica.Models;

namespace ProyectoDeProbabilidadYEstadistica.Services.TStudent;

public class TStudentService : ITStudentService
{
    private const int ChartWidth = 900;
    private const int ChartHeight = 420;
    private const int SampleCount = 281;
    private static readonly string[] AllowedTestTypes = ["two-tailed", "left-tailed", "right-tailed"];

    public CalculationResult<IReadOnlyList<double>> TryParseValues(string rawValues)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(rawValues))
        {
            AddError(errors, nameof(TStudentInputViewModel.RawValues), "Ingresa una coleccion de datos observados.");
            return CalculationResult<IReadOnlyList<double>>.Failure(errors);
        }

        var parsedValues = rawValues
            .Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => double.TryParse(token, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var invariantValue)
                ? (Success: true, Value: invariantValue)
                : double.TryParse(token, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var currentCultureValue)
                    ? (Success: true, Value: currentCultureValue)
                    : (Success: false, Value: 0d))
            .ToList();

        if (parsedValues.Count == 0 || parsedValues.All(item => !item.Success))
        {
            AddError(errors, nameof(TStudentInputViewModel.RawValues), "No fue posible interpretar los valores numericos.");
        }

        if (parsedValues.Any(item => !item.Success))
        {
            AddError(errors, nameof(TStudentInputViewModel.RawValues), "Solo se permiten numeros separados por comas, espacios, punto y coma o saltos de linea.");
        }

        if (errors.Count > 0)
        {
            return CalculationResult<IReadOnlyList<double>>.Failure(errors);
        }

        return CalculationResult<IReadOnlyList<double>>.Success(parsedValues.Select(item => item.Value).ToArray());
    }

    public CalculationResult<TStudentResultViewModel> TryCalculate(TStudentCalculationRequest request)
    {
        var errors = new Dictionary<string, List<string>>();
        var testType = NormalizeTestType(request.TestType);
        var values = request.Values?.ToArray() ?? [];
        var hypothesizedMean = request.HypothesizedMean ?? 0d;
        var alpha = request.Alpha ?? 0d;

        if (values.Length == 0)
        {
            AddError(errors, nameof(request.Values), "Ingresa una coleccion de datos observados.");
        }

        if (values.Any(value => double.IsNaN(value) || double.IsInfinity(value)))
        {
            AddError(errors, nameof(request.Values), "Todos los valores observados deben ser numeros finitos.");
        }

        if (values.Length == 1)
        {
            AddError(errors, nameof(request.Values), "Debes ingresar al menos 2 observaciones.");
        }

        if (values.Length > 1 && values.All(value => NearlyEquals(value, values[0])))
        {
            AddError(errors, nameof(request.Values), "La muestra necesita variacion para calcular la prueba t.");
        }

        if (!request.HypothesizedMean.HasValue || double.IsNaN(hypothesizedMean) || double.IsInfinity(hypothesizedMean))
        {
            AddError(errors, nameof(request.HypothesizedMean), "Ingresa un valor valido para mu0.");
        }

        if (!request.Alpha.HasValue || double.IsNaN(alpha) || double.IsInfinity(alpha) || alpha <= 0d || alpha >= 1d)
        {
            AddError(errors, nameof(request.Alpha), "Ingresa un alpha valido entre 0 y 1.");
        }

        if (!AllowedTestTypes.Contains(testType))
        {
            AddError(errors, nameof(request.TestType), "Selecciona una hipotesis alternativa valida.");
        }

        if (errors.Count > 0)
        {
            return CalculationResult<TStudentResultViewModel>.Failure(errors);
        }

        return CalculationResult<TStudentResultViewModel>.Success(BuildResult(values, hypothesizedMean, alpha, testType));
    }

    public CalculationResult<string> TryBuildSvg(int degreesOfFreedom, double tStatistic, double criticalValue, string testType)
    {
        var errors = new Dictionary<string, List<string>>();
        var normalizedTestType = NormalizeTestType(testType);

        if (degreesOfFreedom < 1)
        {
            AddError(errors, nameof(degreesOfFreedom), "Los grados de libertad deben ser mayores o iguales a 1.");
        }

        if (double.IsNaN(tStatistic) || double.IsInfinity(tStatistic))
        {
            AddError(errors, nameof(tStatistic), "Ingresa un estadistico t valido.");
        }

        if (double.IsNaN(criticalValue) || double.IsInfinity(criticalValue))
        {
            AddError(errors, nameof(criticalValue), "Ingresa un valor critico valido.");
        }

        if (!AllowedTestTypes.Contains(normalizedTestType))
        {
            AddError(errors, nameof(testType), "Selecciona una hipotesis alternativa valida.");
        }

        if (errors.Count > 0)
        {
            return CalculationResult<string>.Failure(errors);
        }

        return CalculationResult<string>.Success(BuildSvg(degreesOfFreedom, tStatistic, criticalValue, normalizedTestType));
    }

    public (string NullHypothesisText, string AlternativeHypothesisText) BuildHypothesisTexts(double hypothesizedMean, string? testType)
    {
        var normalizedTestType = NormalizeTestType(testType);
        var mu0Text = hypothesizedMean.ToString("0.###", CultureInfo.InvariantCulture);
        return ($"H0: mu = {mu0Text}", $"H1: {BuildAlternativeHypothesis(normalizedTestType, mu0Text)}");
    }

    private TStudentResultViewModel BuildResult(IReadOnlyList<double> values, double hypothesizedMean, double alpha, string testType)
    {
        var sampleSize = values.Count;
        var sampleMean = values.Average();
        var sumOfSquares = values.Sum(value => Math.Pow(value - sampleMean, 2d));
        var sampleVariance = sumOfSquares / (sampleSize - 1);
        var sampleStandardDeviation = Math.Sqrt(sampleVariance);
        var degreesOfFreedom = sampleSize - 1;
        var standardError = sampleStandardDeviation / Math.Sqrt(sampleSize);
        var tStatistic = (sampleMean - hypothesizedMean) / standardError;
        var criticalValue = GetCriticalValue(degreesOfFreedom, alpha, testType);
        var pValue = GetPValue(tStatistic, degreesOfFreedom, testType);
        var confidenceCritical = InverseStudentTCdf(1d - (alpha / 2d), degreesOfFreedom);
        var confidenceMargin = confidenceCritical * standardError;
        var hypotheses = BuildHypothesisTexts(hypothesizedMean, testType);

        return new TStudentResultViewModel
        {
            Values = values,
            SampleSize = sampleSize,
            SampleMean = sampleMean,
            SampleStandardDeviation = sampleStandardDeviation,
            DegreesOfFreedom = degreesOfFreedom,
            HypothesizedMean = hypothesizedMean,
            Alpha = alpha,
            TestType = testType,
            NullHypothesisText = hypotheses.NullHypothesisText,
            AlternativeHypothesisText = hypotheses.AlternativeHypothesisText,
            TStatistic = tStatistic,
            CriticalValue = criticalValue,
            PValue = pValue,
            ConfidenceIntervalLower = sampleMean - confidenceMargin,
            ConfidenceIntervalUpper = sampleMean + confidenceMargin,
            Conclusion = pValue <= alpha ? "Se rechaza H0." : "No se rechaza H0.",
            Svg = BuildSvg(degreesOfFreedom, tStatistic, criticalValue, testType)
        };
    }

    private static string BuildAlternativeHypothesis(string testType, string mu0Text)
    {
        return testType switch
        {
            "left-tailed" => $"mu < {mu0Text}",
            "right-tailed" => $"mu > {mu0Text}",
            _ => $"mu != {mu0Text}"
        };
    }

    private static string NormalizeTestType(string? testType)
    {
        return testType?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static double GetCriticalValue(int degreesOfFreedom, double alpha, string testType)
    {
        return testType switch
        {
            "left-tailed" => InverseStudentTCdf(alpha, degreesOfFreedom),
            "right-tailed" => InverseStudentTCdf(1d - alpha, degreesOfFreedom),
            _ => InverseStudentTCdf(1d - (alpha / 2d), degreesOfFreedom)
        };
    }

    private static double GetPValue(double tStatistic, int degreesOfFreedom, string testType)
    {
        var cdf = StudentTCdf(tStatistic, degreesOfFreedom);

        return testType switch
        {
            "left-tailed" => cdf,
            "right-tailed" => 1d - cdf,
            _ => 2d * Math.Min(cdf, 1d - cdf)
        };
    }

    private static string BuildSvg(int degreesOfFreedom, double tStatistic, double criticalValue, string testType)
    {
        const int left = 64;
        const int right = 24;
        const int top = 24;
        const int bottom = 48;

        var xExtent = Math.Clamp(Math.Max(4d, Math.Max(Math.Abs(tStatistic), Math.Abs(criticalValue)) + 1.5d), 4d, 10d);
        var xMin = -xExtent;
        var xMax = xExtent;
        var points = BuildCurvePoints(degreesOfFreedom, xMin, xMax);
        var plotWidth = ChartWidth - left - right;
        var plotHeight = ChartHeight - top - bottom;
        var yMax = points.Max(point => point.Y);

        double ScaleX(double x) => left + ((x - xMin) / (xMax - xMin) * plotWidth);
        double ScaleY(double y) => top + (plotHeight - ((y / yMax) * plotHeight));

        var baselineY = top + plotHeight;
        var curvePoints = string.Join(" ",
            points.Select(point => $"{ScaleX(point.X).ToString("0.##", CultureInfo.InvariantCulture)},{ScaleY(point.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));

        var shadedPolygon = BuildShadedPolygon(points, degreesOfFreedom, tStatistic, testType, ScaleX, ScaleY, baselineY);
        var tLineX = ScaleX(Math.Clamp(tStatistic, xMin, xMax)).ToString("0.##", CultureInfo.InvariantCulture);
        var criticalX = ScaleX(Math.Clamp(criticalValue, xMin, xMax)).ToString("0.##", CultureInfo.InvariantCulture);
        var negativeCriticalX = ScaleX(Math.Clamp(-criticalValue, xMin, xMax)).ToString("0.##", CultureInfo.InvariantCulture);

        return $"""
<svg xmlns="http://www.w3.org/2000/svg" width="{ChartWidth}" height="{ChartHeight}" viewBox="0 0 {ChartWidth} {ChartHeight}" role="img" aria-label="Distribucion t de Student con region sombreada">
  <rect width="100%" height="100%" fill="#f8fafc" rx="18" />
  <rect x="{left}" y="{top}" width="{plotWidth}" height="{plotHeight}" fill="#fff" stroke="#d1d5db" />
  {shadedPolygon}
  <polyline fill="none" stroke="#0f172a" stroke-width="3" points="{curvePoints}" />
  <line x1="{left}" y1="{baselineY}" x2="{left + plotWidth}" y2="{baselineY}" stroke="#111827" stroke-width="1.4" />
  <line x1="{ScaleX(0d).ToString("0.##", CultureInfo.InvariantCulture)}" y1="{top}" x2="{ScaleX(0d).ToString("0.##", CultureInfo.InvariantCulture)}" y2="{baselineY}" stroke="#cbd5e1" stroke-width="1" />
  <line x1="{tLineX}" y1="{top}" x2="{tLineX}" y2="{baselineY}" stroke="#b91c1c" stroke-width="2" stroke-dasharray="6 4" />
  {BuildCriticalLines(testType, criticalX, negativeCriticalX, top, baselineY)}
  <text x="{tLineX}" y="{top + 18}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="13" fill="#991b1b">t = {tStatistic.ToString("0.###", CultureInfo.InvariantCulture)}</text>
  <text x="{left + (plotWidth / 2)}" y="{ChartHeight - 10}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="14" fill="#374151">Valores t</text>
  <text x="18" y="{top + (plotHeight / 2)}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="14" fill="#374151" transform="rotate(-90 18 {top + (plotHeight / 2)})">Densidad</text>
  <text x="{ChartWidth - 160}" y="{top + 22}" font-family="Arial, Helvetica, sans-serif" font-size="13" fill="#374151">df = {degreesOfFreedom}</text>
  {BuildXAxisLabels(plotWidth, left, baselineY, xMin, xMax)}
</svg>
""";
    }

    private static string BuildCriticalLines(string testType, string criticalX, string negativeCriticalX, int top, int baselineY)
    {
        if (testType == "two-tailed")
        {
            return $"""
  <line x1="{criticalX}" y1="{top}" x2="{criticalX}" y2="{baselineY}" stroke="#1d4ed8" stroke-width="2" stroke-dasharray="4 4" />
  <line x1="{negativeCriticalX}" y1="{top}" x2="{negativeCriticalX}" y2="{baselineY}" stroke="#1d4ed8" stroke-width="2" stroke-dasharray="4 4" />
""";
        }

        return $"""
  <line x1="{criticalX}" y1="{top}" x2="{criticalX}" y2="{baselineY}" stroke="#1d4ed8" stroke-width="2" stroke-dasharray="4 4" />
""";
    }

    private static string BuildShadedPolygon(
        IReadOnlyList<(double X, double Y)> points,
        int degreesOfFreedom,
        double tStatistic,
        string testType,
        Func<double, double> scaleX,
        Func<double, double> scaleY,
        int baselineY)
    {
        var regions = new List<List<(double X, double Y)>>();

        if (testType == "left-tailed")
        {
            regions.Add(BuildSingleRegion(points, degreesOfFreedom, points.First().X, tStatistic));
        }
        else if (testType == "right-tailed")
        {
            regions.Add(BuildSingleRegion(points, degreesOfFreedom, tStatistic, points.Last().X));
        }
        else
        {
            var boundary = Math.Abs(tStatistic);
            regions.Add(BuildSingleRegion(points, degreesOfFreedom, points.First().X, -boundary));
            regions.Add(BuildSingleRegion(points, degreesOfFreedom, boundary, points.Last().X));
        }

        var polygons = new List<string>();
        foreach (var region in regions.Where(region => region.Count > 0))
        {
            var start = region.First();
            var end = region.Last();
            var polygonPoints = new List<string>
            {
                $"{scaleX(start.X).ToString("0.##", CultureInfo.InvariantCulture)},{baselineY.ToString(CultureInfo.InvariantCulture)}"
            };

            polygonPoints.AddRange(region.Select(point =>
                $"{scaleX(point.X).ToString("0.##", CultureInfo.InvariantCulture)},{scaleY(point.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));

            polygonPoints.Add($"{scaleX(end.X).ToString("0.##", CultureInfo.InvariantCulture)},{baselineY.ToString(CultureInfo.InvariantCulture)}");
            polygons.Add($"<polygon points=\"{string.Join(" ", polygonPoints)}\" fill=\"#0f766e\" fill-opacity=\"0.32\" />");
        }

        return string.Join(Environment.NewLine, polygons);
    }

    private static List<(double X, double Y)> BuildSingleRegion(IReadOnlyList<(double X, double Y)> points, int degreesOfFreedom, double startX, double endX)
    {
        if (endX < startX)
        {
            (startX, endX) = (endX, startX);
        }

        var firstX = points.First().X;
        var lastX = points.Last().X;
        startX = Math.Clamp(startX, firstX, lastX);
        endX = Math.Clamp(endX, firstX, lastX);

        var region = new List<(double X, double Y)>
        {
            (startX, StudentTPdf(startX, degreesOfFreedom))
        };

        region.AddRange(points.Where(point => point.X > startX && point.X < endX));
        region.Add((endX, StudentTPdf(endX, degreesOfFreedom)));
        return region;
    }

    private static List<(double X, double Y)> BuildCurvePoints(int degreesOfFreedom, double xMin, double xMax)
    {
        var step = (xMax - xMin) / (SampleCount - 1);
        var points = new List<(double X, double Y)>(SampleCount);

        for (var index = 0; index < SampleCount; index++)
        {
            var x = xMin + (step * index);
            points.Add((x, StudentTPdf(x, degreesOfFreedom)));
        }

        return points;
    }

    private static string BuildXAxisLabels(int plotWidth, int left, int baselineY, double xMin, double xMax)
    {
        var builder = new StringBuilder();
        var start = (int)Math.Ceiling(xMin);
        var end = (int)Math.Floor(xMax);

        for (var value = start; value <= end; value++)
        {
            var x = left + (((value - xMin) / (xMax - xMin)) * plotWidth);
            builder.AppendLine(
                $"<text x=\"{x.ToString("0.##", CultureInfo.InvariantCulture)}\" y=\"{(baselineY + 20).ToString(CultureInfo.InvariantCulture)}\" text-anchor=\"middle\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"12\" fill=\"#4b5563\">{value.ToString(CultureInfo.InvariantCulture)}</text>");
        }

        return builder.ToString();
    }

    private static double StudentTPdf(double x, int degreesOfFreedom)
    {
        var v = degreesOfFreedom;
        var logNumerator = LogGamma((v + 1d) / 2d);
        var logDenominator = 0.5d * Math.Log(v * Math.PI) + LogGamma(v / 2d);
        var logPower = -((v + 1d) / 2d) * Math.Log(1d + ((x * x) / v));
        return Math.Exp(logNumerator - logDenominator + logPower);
    }

    private static double StudentTCdf(double t, int degreesOfFreedom)
    {
        if (t == 0d)
        {
            return 0.5d;
        }

        var v = degreesOfFreedom;
        var x = v / (v + (t * t));
        var beta = RegularizedIncompleteBeta(x, v / 2d, 0.5d);
        return t > 0d ? 1d - (0.5d * beta) : 0.5d * beta;
    }

    private static double InverseStudentTCdf(double probability, int degreesOfFreedom)
    {
        if (probability <= 0d)
        {
            return double.NegativeInfinity;
        }

        if (probability >= 1d)
        {
            return double.PositiveInfinity;
        }

        var lower = -50d;
        var upper = 50d;

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var middle = (lower + upper) / 2d;
            var cdf = StudentTCdf(middle, degreesOfFreedom);

            if (cdf < probability)
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }
        }

        return (lower + upper) / 2d;
    }

    private static double RegularizedIncompleteBeta(double x, double a, double b)
    {
        if (x <= 0d)
        {
            return 0d;
        }

        if (x >= 1d)
        {
            return 1d;
        }

        var bt = Math.Exp(LogGamma(a + b) - LogGamma(a) - LogGamma(b) + (a * Math.Log(x)) + (b * Math.Log(1d - x)));

        if (x < (a + 1d) / (a + b + 2d))
        {
            return bt * BetaContinuedFraction(x, a, b) / a;
        }

        return 1d - (bt * BetaContinuedFraction(1d - x, b, a) / b);
    }

    private static double BetaContinuedFraction(double x, double a, double b)
    {
        const int maxIterations = 200;
        const double epsilon = 3e-14;
        const double tiny = 1e-30;

        var qab = a + b;
        var qap = a + 1d;
        var qam = a - 1d;
        var c = 1d;
        var d = 1d - ((qab * x) / qap);

        if (Math.Abs(d) < tiny)
        {
            d = tiny;
        }

        d = 1d / d;
        var h = d;

        for (var m = 1; m <= maxIterations; m++)
        {
            var m2 = 2 * m;
            var aa = (m * (b - m) * x) / ((qam + m2) * (a + m2));
            d = 1d + (aa * d);
            if (Math.Abs(d) < tiny)
            {
                d = tiny;
            }

            c = 1d + (aa / c);
            if (Math.Abs(c) < tiny)
            {
                c = tiny;
            }

            d = 1d / d;
            h *= d * c;

            aa = -((a + m) * (qab + m) * x) / ((a + m2) * (qap + m2));
            d = 1d + (aa * d);
            if (Math.Abs(d) < tiny)
            {
                d = tiny;
            }

            c = 1d + (aa / c);
            if (Math.Abs(c) < tiny)
            {
                c = tiny;
            }

            d = 1d / d;
            var delta = d * c;
            h *= delta;

            if (Math.Abs(delta - 1d) < epsilon)
            {
                break;
            }
        }

        return h;
    }

    private static double LogGamma(double value)
    {
        double[] coefficients =
        [
            76.18009172947146d,
            -86.50532032941677d,
            24.01409824083091d,
            -1.231739572450155d,
            0.001208650973866179d,
            -0.000005395239384953d
        ];

        var x = value;
        var y = value;
        var tmp = x + 5.5d;
        tmp -= (x + 0.5d) * Math.Log(tmp);
        var series = 1.000000000190015d;

        foreach (var coefficient in coefficients)
        {
            y += 1d;
            series += coefficient / y;
        }

        return -tmp + Math.Log(2.5066282746310005d * series / x);
    }

    private static bool NearlyEquals(double left, double right)
    {
        return Math.Abs(left - right) < 1e-12d;
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
