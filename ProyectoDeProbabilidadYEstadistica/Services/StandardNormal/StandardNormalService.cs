using System.Globalization;
using System.Text;
using ProyectoDeProbabilidadYEstadistica.Models;

namespace ProyectoDeProbabilidadYEstadistica.Services.StandardNormal;

public class StandardNormalService : IStandardNormalService
{
    private const int ChartWidth = 900;
    private const int ChartHeight = 420;
    private const int SampleCount = 241;
    private const double XMin = -4d;
    private const double XMax = 4d;
    private static readonly string[] AllowedCalculationModes = ["from-z", "from-area"];
    private static readonly string[] AllowedFormModes = ["normal", "advanced"];

    public CalculationResult<StandardNormalResultViewModel> TryCalculate(StandardNormalCalculationRequest request)
    {
        var errors = new Dictionary<string, List<string>>();
        var formMode = NormalizeFormMode(request.FormMode);
        var calculationMode = NormalizeCalculationMode(request.CalculationMode);
        var areaSide = NormalizeAreaSide(request.AreaSide);
        var zValue = 0d;
        var areaValue = request.AreaValue ?? 0d;

        if (!AllowedFormModes.Contains(formMode))
        {
            AddError(errors, nameof(request.FormMode), "Selecciona un modo valido.");
        }

        if (formMode == "advanced")
        {
            return errors.Count > 0
                ? CalculationResult<StandardNormalResultViewModel>.Failure(errors)
                : TryCalculateRange(request.IntervalRange);
        }

        if (!AllowedCalculationModes.Contains(calculationMode))
        {
            AddError(errors, nameof(request.CalculationMode), "Selecciona un tipo de calculo valido.");
        }

        if (!IsValidAreaSide(areaSide))
        {
            AddError(errors, nameof(request.AreaSide), "Selecciona si deseas el area antes o despues de z.");
        }

        if (calculationMode == "from-z")
        {
            zValue = request.ZValue ?? 0d;

            if (!request.ZValue.HasValue || double.IsNaN(zValue) || double.IsInfinity(zValue))
            {
                AddError(errors, nameof(request.ZValue), "Ingresa un valor z valido.");
            }
        }
        else if (calculationMode == "from-area")
        {
            if (!request.AreaValue.HasValue || double.IsNaN(areaValue) || double.IsInfinity(areaValue))
            {
                AddError(errors, nameof(request.AreaValue), "Ingresa un area valida.");
            }
            else if (areaValue <= 0d || areaValue >= 1d)
            {
                AddError(errors, nameof(request.AreaValue), "El area debe ser mayor que 0 y menor que 1.");
            }
            else if (!errors.ContainsKey(nameof(request.AreaSide)))
            {
                zValue = areaSide == "left"
                    ? InverseStandardNormalCdf(areaValue)
                    : InverseStandardNormalCdf(1d - areaValue);
            }
        }

        if (errors.Count > 0)
        {
            return CalculationResult<StandardNormalResultViewModel>.Failure(errors);
        }

        return CalculationResult<StandardNormalResultViewModel>.Success(BuildNormalResult(calculationMode, zValue, areaSide));
    }

    public CalculationResult<StandardNormalResultViewModel> TryCalculateRange(string intervalRange)
    {
        var errors = new Dictionary<string, List<string>>();

        if (!TryParseIntervalRange(intervalRange, out var lowerBound, out var upperBound, out var errorMessage))
        {
            AddError(errors, nameof(intervalRange), errorMessage);
            return CalculationResult<StandardNormalResultViewModel>.Failure(errors);
        }

        var area = StandardNormalCdf(upperBound) - StandardNormalCdf(lowerBound);
        var normalizedRange = BuildIntervalRangeLabel(lowerBound, upperBound);

        return CalculationResult<StandardNormalResultViewModel>.Success(new StandardNormalResultViewModel
        {
            FormMode = "advanced",
            CalculationMode = "range-area",
            AreaSide = "between",
            Area = area,
            ComplementArea = 1d - area,
            IntervalRange = normalizedRange,
            LowerBound = double.IsInfinity(lowerBound) ? null : lowerBound,
            UpperBound = double.IsInfinity(upperBound) ? null : upperBound,
            Svg = BuildRangeSvg(lowerBound, upperBound)
        });
    }

    public CalculationResult<string> TryBuildSvg(double zValue, string areaSide)
    {
        var normalizedAreaSide = NormalizeAreaSide(areaSide);
        var errors = new Dictionary<string, List<string>>();

        if (double.IsNaN(zValue) || double.IsInfinity(zValue))
        {
            AddError(errors, nameof(zValue), "Ingresa un valor z valido.");
        }

        if (!IsValidAreaSide(normalizedAreaSide))
        {
            AddError(errors, nameof(areaSide), "Selecciona un lado de area valido.");
        }

        if (errors.Count > 0)
        {
            return CalculationResult<string>.Failure(errors);
        }

        return CalculationResult<string>.Success(BuildSvg(zValue, normalizedAreaSide));
    }

    public CalculationResult<string> TryBuildRangeSvg(string intervalRange)
    {
        var errors = new Dictionary<string, List<string>>();

        if (!TryParseIntervalRange(intervalRange, out var lowerBound, out var upperBound, out var errorMessage))
        {
            AddError(errors, nameof(intervalRange), errorMessage);
            return CalculationResult<string>.Failure(errors);
        }

        return CalculationResult<string>.Success(BuildRangeSvg(lowerBound, upperBound));
    }

    private static StandardNormalResultViewModel BuildNormalResult(string calculationMode, double zValue, string areaSide)
    {
        var cdf = StandardNormalCdf(zValue);
        var area = areaSide == "left" ? cdf : 1d - cdf;

        return new StandardNormalResultViewModel
        {
            FormMode = "normal",
            CalculationMode = calculationMode,
            ZValue = zValue,
            AreaSide = areaSide,
            Area = area,
            ComplementArea = 1d - area,
            Svg = BuildSvg(zValue, areaSide)
        };
    }

    private static string BuildSvgLayout(
        string shadedPolygon,
        string curvePoints,
        string overlayMarkup,
        string ariaLabel)
    {
        const int left = 64;
        const int right = 24;
        const int top = 24;
        const int bottom = 48;

        var plotWidth = ChartWidth - left - right;
        var plotHeight = ChartHeight - top - bottom;

        double ScaleX(double x) => left + ((x - XMin) / (XMax - XMin) * plotWidth);

        var baselineY = top + plotHeight;

        return $"""
<svg xmlns="http://www.w3.org/2000/svg" width="{ChartWidth}" height="{ChartHeight}" viewBox="0 0 {ChartWidth} {ChartHeight}" role="img" aria-label="{ariaLabel}">
  <rect width="100%" height="100%" fill="#f8fafc" rx="18" />
  <rect x="{left}" y="{top}" width="{plotWidth}" height="{plotHeight}" fill="#fff" stroke="#d1d5db" />
  <polygon points="{shadedPolygon}" fill="#16a34a" fill-opacity="0.32" />
  <polyline fill="none" stroke="#0f172a" stroke-width="3" points="{curvePoints}" />
  <line x1="{left}" y1="{baselineY}" x2="{left + plotWidth}" y2="{baselineY}" stroke="#111827" stroke-width="1.4" />
  <line x1="{ScaleX(0d).ToString("0.##", CultureInfo.InvariantCulture)}" y1="{top}" x2="{ScaleX(0d).ToString("0.##", CultureInfo.InvariantCulture)}" y2="{baselineY}" stroke="#cbd5e1" stroke-dasharray="4 4" />
  {overlayMarkup}
  <text x="{left + (plotWidth / 2)}" y="{ChartHeight - 10}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="14" fill="#374151">Valor z</text>
  <text x="18" y="{top + (plotHeight / 2)}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="14" fill="#374151" transform="rotate(-90 18 {top + (plotHeight / 2)})">Densidad</text>
  {BuildXAxisLabels(plotWidth, left, baselineY)}
</svg>
""";
    }

    private static string BuildSvg(double zValue, string areaSide)
    {
        const int left = 64;
        const int right = 24;
        const int top = 24;
        const int bottom = 48;

        var points = BuildCurvePoints();
        var plotWidth = ChartWidth - left - right;
        var plotHeight = ChartHeight - top - bottom;
        var yMax = StandardNormalPdf(0d);

        double ScaleX(double x) => left + ((x - XMin) / (XMax - XMin) * plotWidth);
        double ScaleY(double y) => top + (plotHeight - ((y / yMax) * plotHeight));

        var baselineY = top + plotHeight;
        var curvePoints = string.Join(" ",
            points.Select(point => $"{ScaleX(point.X).ToString("0.##", CultureInfo.InvariantCulture)},{ScaleY(point.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));
        var shadedPolygon = BuildTailShadedPolygon(points, zValue, areaSide, ScaleX, ScaleY, baselineY);
        var zLineX = ScaleX(Math.Clamp(zValue, XMin, XMax)).ToString("0.##", CultureInfo.InvariantCulture);
        var zLabelY = (top + 18).ToString(CultureInfo.InvariantCulture);
        var zLabel = $"z = {zValue.ToString("0.###", CultureInfo.InvariantCulture)}";
        var overlayMarkup = $"""
<line x1="{zLineX}" y1="{top}" x2="{zLineX}" y2="{baselineY}" stroke="#15803d" stroke-width="2" stroke-dasharray="6 4" />
  <text x="{zLineX}" y="{zLabelY}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="13" fill="#166534">{zLabel}</text>
""";

        return BuildSvgLayout(shadedPolygon, curvePoints, overlayMarkup, "Distribucion normal estandar con area sombreada");
    }

    private static string BuildRangeSvg(double lowerBound, double upperBound)
    {
        const int left = 64;
        const int right = 24;
        const int top = 24;
        const int bottom = 48;

        var points = BuildCurvePoints();
        var plotWidth = ChartWidth - left - right;
        var plotHeight = ChartHeight - top - bottom;
        var yMax = StandardNormalPdf(0d);

        double ScaleX(double x) => left + ((x - XMin) / (XMax - XMin) * plotWidth);
        double ScaleY(double y) => top + (plotHeight - ((y / yMax) * plotHeight));

        var baselineY = top + plotHeight;
        var curvePoints = string.Join(" ",
            points.Select(point => $"{ScaleX(point.X).ToString("0.##", CultureInfo.InvariantCulture)},{ScaleY(point.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));
        var shadedPolygon = BuildRangeShadedPolygon(points, lowerBound, upperBound, ScaleX, ScaleY, baselineY);
        var overlayMarkup = BuildRangeOverlay(lowerBound, upperBound, ScaleX, top, baselineY);

        return BuildSvgLayout(shadedPolygon, curvePoints, overlayMarkup, "Distribucion normal estandar con area sombreada en un intervalo");
    }

    private static string BuildTailShadedPolygon(
        IReadOnlyList<(double X, double Y)> points,
        double zValue,
        string areaSide,
        Func<double, double> scaleX,
        Func<double, double> scaleY,
        int baselineY)
    {
        var boundary = Math.Clamp(zValue, XMin, XMax);
        var region = new List<(double X, double Y)>();

        if (areaSide == "left")
        {
            var endX = boundary;
            region.Add((XMin, StandardNormalPdf(XMin)));
            region.AddRange(points.Where(point => point.X > XMin && point.X < endX));
            region.Add((endX, StandardNormalPdf(endX)));
        }
        else
        {
            var startX = boundary;
            region.Add((startX, StandardNormalPdf(startX)));
            region.AddRange(points.Where(point => point.X > startX && point.X < XMax));
            region.Add((XMax, StandardNormalPdf(XMax)));
        }

        if (zValue <= XMin && areaSide == "left")
        {
            region.Clear();
            region.Add((XMin, StandardNormalPdf(XMin)));
        }

        if (zValue >= XMax && areaSide == "right")
        {
            region.Clear();
            region.Add((XMax, StandardNormalPdf(XMax)));
        }

        var polygonPoints = new List<string>();
        var start = region.First();
        var end = region.Last();

        polygonPoints.Add($"{scaleX(start.X).ToString("0.##", CultureInfo.InvariantCulture)},{baselineY.ToString(CultureInfo.InvariantCulture)}");
        polygonPoints.AddRange(region.Select(point =>
            $"{scaleX(point.X).ToString("0.##", CultureInfo.InvariantCulture)},{scaleY(point.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));
        polygonPoints.Add($"{scaleX(end.X).ToString("0.##", CultureInfo.InvariantCulture)},{baselineY.ToString(CultureInfo.InvariantCulture)}");

        return string.Join(" ", polygonPoints);
    }

    private static string BuildRangeShadedPolygon(
        IReadOnlyList<(double X, double Y)> points,
        double lowerBound,
        double upperBound,
        Func<double, double> scaleX,
        Func<double, double> scaleY,
        int baselineY)
    {
        var startX = Math.Clamp(lowerBound, XMin, XMax);
        var endX = Math.Clamp(upperBound, XMin, XMax);
        var region = new List<(double X, double Y)>();

        region.Add((startX, StandardNormalPdf(startX)));
        region.AddRange(points.Where(point => point.X > startX && point.X < endX));
        region.Add((endX, StandardNormalPdf(endX)));

        if (startX >= endX)
        {
            region.Clear();
            region.Add((startX, StandardNormalPdf(startX)));
        }

        var polygonPoints = new List<string>();
        var start = region.First();
        var end = region.Last();

        polygonPoints.Add($"{scaleX(start.X).ToString("0.##", CultureInfo.InvariantCulture)},{baselineY.ToString(CultureInfo.InvariantCulture)}");
        polygonPoints.AddRange(region.Select(point =>
            $"{scaleX(point.X).ToString("0.##", CultureInfo.InvariantCulture)},{scaleY(point.Y).ToString("0.##", CultureInfo.InvariantCulture)}"));
        polygonPoints.Add($"{scaleX(end.X).ToString("0.##", CultureInfo.InvariantCulture)},{baselineY.ToString(CultureInfo.InvariantCulture)}");

        return string.Join(" ", polygonPoints);
    }

    private static string BuildRangeOverlay(
        double lowerBound,
        double upperBound,
        Func<double, double> scaleX,
        int top,
        int baselineY)
    {
        var parts = new List<string>();

        if (!double.IsNegativeInfinity(lowerBound))
        {
            var lowerX = scaleX(Math.Clamp(lowerBound, XMin, XMax)).ToString("0.##", CultureInfo.InvariantCulture);
            parts.Add($"""<line x1="{lowerX}" y1="{top}" x2="{lowerX}" y2="{baselineY}" stroke="#15803d" stroke-width="2" stroke-dasharray="6 4" />""");
            parts.Add($"""<text x="{lowerX}" y="{(top + 18).ToString(CultureInfo.InvariantCulture)}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="13" fill="#166534">z1 = {lowerBound.ToString("0.###", CultureInfo.InvariantCulture)}</text>""");
        }

        if (!double.IsPositiveInfinity(upperBound))
        {
            var upperX = scaleX(Math.Clamp(upperBound, XMin, XMax)).ToString("0.##", CultureInfo.InvariantCulture);
            parts.Add($"""<line x1="{upperX}" y1="{top}" x2="{upperX}" y2="{baselineY}" stroke="#15803d" stroke-width="2" stroke-dasharray="6 4" />""");
            parts.Add($"""<text x="{upperX}" y="{(top + 36).ToString(CultureInfo.InvariantCulture)}" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="13" fill="#166534">z2 = {upperBound.ToString("0.###", CultureInfo.InvariantCulture)}</text>""");
        }

        return string.Join(Environment.NewLine + "  ", parts);
    }

    private static string BuildXAxisLabels(int plotWidth, int left, int baselineY)
    {
        var builder = new StringBuilder();

        for (var value = -4; value <= 4; value++)
        {
            var x = left + (((value - XMin) / (XMax - XMin)) * plotWidth);
            builder.AppendLine(
                $"<text x=\"{x.ToString("0.##", CultureInfo.InvariantCulture)}\" y=\"{(baselineY + 20).ToString(CultureInfo.InvariantCulture)}\" text-anchor=\"middle\" font-family=\"Arial, Helvetica, sans-serif\" font-size=\"12\" fill=\"#4b5563\">{value.ToString(CultureInfo.InvariantCulture)}</text>");
        }

        return builder.ToString();
    }

    private static List<(double X, double Y)> BuildCurvePoints()
    {
        var step = (XMax - XMin) / (SampleCount - 1);
        var points = new List<(double X, double Y)>(SampleCount);

        for (var index = 0; index < SampleCount; index++)
        {
            var x = XMin + (step * index);
            points.Add((x, StandardNormalPdf(x)));
        }

        return points;
    }

    private static double StandardNormalPdf(double x)
    {
        return Math.Exp(-0.5d * x * x) / Math.Sqrt(2d * Math.PI);
    }

    private static double StandardNormalCdf(double z)
    {
        return 0.5d * (1d + Erf(z / Math.Sqrt(2d)));
    }

    private static double InverseStandardNormalCdf(double probability)
    {
        if (probability <= 0d || probability >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), "The probability must be between 0 and 1.");
        }

        const double a1 = -39.6968302866538d;
        const double a2 = 220.946098424521d;
        const double a3 = -275.928510446969d;
        const double a4 = 138.357751867269d;
        const double a5 = -30.6647980661472d;
        const double a6 = 2.50662827745924d;

        const double b1 = -54.4760987982241d;
        const double b2 = 161.585836858041d;
        const double b3 = -155.698979859887d;
        const double b4 = 66.8013118877197d;
        const double b5 = -13.2806815528857d;

        const double c1 = -0.00778489400243029d;
        const double c2 = -0.322396458041136d;
        const double c3 = -2.40075827716184d;
        const double c4 = -2.54973253934373d;
        const double c5 = 4.37466414146497d;
        const double c6 = 2.93816398269878d;

        const double d1 = 0.00778469570904146d;
        const double d2 = 0.32246712907004d;
        const double d3 = 2.445134137143d;
        const double d4 = 3.75440866190742d;

        const double lowerRegion = 0.02425d;
        const double upperRegion = 1d - lowerRegion;

        if (probability < lowerRegion)
        {
            var q = Math.Sqrt(-2d * Math.Log(probability));
            var numerator = (((((c1 * q) + c2) * q + c3) * q + c4) * q + c5) * q + c6;
            var denominator = ((((d1 * q) + d2) * q + d3) * q + d4) * q + 1d;
            return numerator / denominator;
        }

        if (probability > upperRegion)
        {
            var q = Math.Sqrt(-2d * Math.Log(1d - probability));
            return -((((((c1 * q) + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                    (((((d1 * q) + d2) * q + d3) * q + d4) * q + 1d);
        }

        var centeredProbability = probability - 0.5d;
        var r = centeredProbability * centeredProbability;
        var centralNumerator = (((((a1 * r) + a2) * r + a3) * r + a4) * r + a5) * r + a6;
        var centralDenominator = (((((b1 * r) + b2) * r + b3) * r + b4) * r + b5) * r + 1d;
        return centeredProbability * (centralNumerator / centralDenominator);
    }

    private static double Erf(double value)
    {
        var sign = Math.Sign(value);
        var x = Math.Abs(value);
        var t = 1d / (1d + (0.3275911d * x));
        var polynomial = (((((1.061405429d * t) - 1.453152027d) * t) + 1.421413741d) * t - 0.284496736d) * t + 0.254829592d;
        var approximation = 1d - (polynomial * t * Math.Exp(-x * x));
        return sign * approximation;
    }

    private static string NormalizeAreaSide(string? areaSide)
    {
        return areaSide?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeCalculationMode(string? calculationMode)
    {
        return calculationMode?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeFormMode(string? formMode)
    {
        return formMode?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static bool IsValidAreaSide(string areaSide)
    {
        return areaSide is "left" or "right";
    }

    private static bool TryParseIntervalRange(string intervalRange, out double lowerBound, out double upperBound, out string errorMessage)
    {
        lowerBound = 0d;
        upperBound = 0d;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(intervalRange))
        {
            errorMessage = "Ingresa un intervalo valido con el formato [a, b].";
            return false;
        }

        var trimmed = intervalRange.Trim();
        if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']'))
        {
            errorMessage = "El intervalo debe usar el formato [a, b].";
            return false;
        }

        var content = trimmed[1..^1];
        var parts = content.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            errorMessage = "El intervalo debe contener exactamente dos limites.";
            return false;
        }

        if (!TryParseBound(parts[0], out lowerBound) || !TryParseBound(parts[1], out upperBound))
        {
            errorMessage = "Los limites del intervalo deben ser numeros o ±Infinity.";
            return false;
        }

        if (double.IsPositiveInfinity(lowerBound))
        {
            errorMessage = "El limite inferior no puede ser +Infinity.";
            return false;
        }

        if (double.IsNegativeInfinity(upperBound))
        {
            errorMessage = "El limite superior no puede ser -Infinity.";
            return false;
        }

        if (lowerBound > upperBound)
        {
            errorMessage = "El limite inferior debe ser menor o igual que el superior.";
            return false;
        }

        return true;
    }

    private static bool TryParseBound(string rawValue, out double value)
    {
        value = 0d;
        var normalized = rawValue.Trim()
            .Replace("−", "-")
            .Replace("∞", "Infinity", StringComparison.Ordinal);

        if (normalized.Equals("Infinity", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("+Infinity", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Inf", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("+Inf", StringComparison.OrdinalIgnoreCase))
        {
            value = double.PositiveInfinity;
            return true;
        }

        if (normalized.Equals("-Infinity", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("-Inf", StringComparison.OrdinalIgnoreCase))
        {
            value = double.NegativeInfinity;
            return true;
        }

        return double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static string BuildIntervalRangeLabel(double lowerBound, double upperBound)
    {
        return $"[{FormatBound(lowerBound)}, {FormatBound(upperBound)}]";
    }

    private static string FormatBound(double value)
    {
        if (double.IsNegativeInfinity(value))
        {
            return "-∞";
        }

        if (double.IsPositiveInfinity(value))
        {
            return "∞";
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
