namespace ProyectoDeProbabilidadYEstadistica.Models;

public class StandardNormalResultViewModel
{
    public string FormMode { get; init; } = "normal";
    public string CalculationMode { get; init; } = "from-z";
    public double? ZValue { get; init; }
    public string AreaSide { get; init; } = "left";
    public double Area { get; init; }
    public double ComplementArea { get; init; }
    public string IntervalRange { get; init; } = string.Empty;
    public double? LowerBound { get; init; }
    public double? UpperBound { get; init; }
    public string Svg { get; init; } = string.Empty;
}
