namespace ProyectoDeProbabilidadYEstadistica.Models;

public class StandardNormalApiResponse
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

    public static StandardNormalApiResponse FromResult(StandardNormalResultViewModel result)
    {
        return new StandardNormalApiResponse
        {
            FormMode = result.FormMode,
            CalculationMode = result.CalculationMode,
            ZValue = result.ZValue,
            AreaSide = result.AreaSide,
            Area = result.Area,
            ComplementArea = result.ComplementArea,
            IntervalRange = result.IntervalRange,
            LowerBound = result.LowerBound,
            UpperBound = result.UpperBound
        };
    }
}
