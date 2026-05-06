namespace ProyectoDeProbabilidadYEstadistica.Models;

public class ExpectedValueResultViewModel
{
    public string FormMode { get; init; } = "normal";
    public IReadOnlyList<double> Values { get; init; } = [];
    public IReadOnlyList<double> Probabilities { get; init; } = [];
    public double ProbabilityTotal { get; init; }
    public double ExpectedValue { get; init; }
}
