namespace ProyectoDeProbabilidadYEstadistica.Models;

public class TStudentApiResponse
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

    public static TStudentApiResponse FromResult(TStudentResultViewModel result)
    {
        return new TStudentApiResponse
        {
            Values = result.Values,
            SampleSize = result.SampleSize,
            SampleMean = result.SampleMean,
            SampleStandardDeviation = result.SampleStandardDeviation,
            DegreesOfFreedom = result.DegreesOfFreedom,
            HypothesizedMean = result.HypothesizedMean,
            Alpha = result.Alpha,
            TestType = result.TestType,
            NullHypothesisText = result.NullHypothesisText,
            AlternativeHypothesisText = result.AlternativeHypothesisText,
            TStatistic = result.TStatistic,
            CriticalValue = result.CriticalValue,
            PValue = result.PValue,
            ConfidenceIntervalLower = result.ConfidenceIntervalLower,
            ConfidenceIntervalUpper = result.ConfidenceIntervalUpper,
            Conclusion = result.Conclusion
        };
    }
}
