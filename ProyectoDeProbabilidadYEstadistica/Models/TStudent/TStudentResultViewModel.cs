namespace ProyectoDeProbabilidadYEstadistica.Models;

public class TStudentResultViewModel
{
    public IReadOnlyList<double> Values { get; init; } = Array.Empty<double>();
    public int SampleSize { get; init; }
    public double SampleMean { get; init; }
    public double SampleStandardDeviation { get; init; }
    public int DegreesOfFreedom { get; init; }
    public double HypothesizedMean { get; init; }
    public double Alpha { get; init; }
    public string TestType { get; init; } = "two-tailed";
    public string NullHypothesisText { get; init; } = string.Empty;
    public string AlternativeHypothesisText { get; init; } = string.Empty;
    public double TStatistic { get; init; }
    public double CriticalValue { get; init; }
    public double PValue { get; init; }
    public double ConfidenceIntervalLower { get; init; }
    public double ConfidenceIntervalUpper { get; init; }
    public string Conclusion { get; init; } = string.Empty;
    public string Svg { get; init; } = string.Empty;
}
