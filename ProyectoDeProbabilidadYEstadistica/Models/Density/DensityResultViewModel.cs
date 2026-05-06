using System.Collections.Generic;

namespace ProyectoDeProbabilidadYEstadistica.Models;

public class DensityResultViewModel
{
    public IReadOnlyList<double> Values { get; init; } = [];
    public IReadOnlyList<DensityPointViewModel> Points { get; init; } = [];
    public double Mean { get; init; }
    public double StandardDeviation { get; init; }
    public double Bandwidth { get; init; }
    public string Svg { get; init; } = string.Empty;
}
