using System.Collections.Generic;

namespace ProyectoDeProbabilidadYEstadistica.Models;

public class VarianceResultViewModel
{
    public IReadOnlyList<double> Values { get; init; } = [];
    public double Mean { get; init; }
    public double Variance { get; init; }
    public double SingleProbability { get; init; }
    public double ExpectedValue { get; init; }
}
