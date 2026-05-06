using ProyectoDeProbabilidadYEstadistica.Models;

namespace ProyectoDeProbabilidadYEstadistica.Services.TStudent;

public interface ITStudentService
{
    CalculationResult<IReadOnlyList<double>> TryParseValues(string rawValues);
    CalculationResult<TStudentResultViewModel> TryCalculate(TStudentCalculationRequest request);
    CalculationResult<string> TryBuildSvg(int degreesOfFreedom, double tStatistic, double criticalValue, string testType);
    (string NullHypothesisText, string AlternativeHypothesisText) BuildHypothesisTexts(double hypothesizedMean, string? testType);
}
