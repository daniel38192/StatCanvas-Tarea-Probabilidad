using ProyectoDeProbabilidadYEstadistica.Models;

namespace ProyectoDeProbabilidadYEstadistica.Services.StandardNormal;

public interface IStandardNormalService
{
    CalculationResult<StandardNormalResultViewModel> TryCalculate(StandardNormalCalculationRequest request);
    CalculationResult<StandardNormalResultViewModel> TryCalculateRange(string intervalRange);
    CalculationResult<string> TryBuildSvg(double zValue, string areaSide);
    CalculationResult<string> TryBuildRangeSvg(string intervalRange);
}
