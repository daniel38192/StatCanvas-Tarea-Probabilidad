namespace ProyectoDeProbabilidadYEstadistica.Models;

public class TStudentCalculationRequest
{
    public IReadOnlyList<double>? Values { get; set; }
    public double? HypothesizedMean { get; set; }
    public double? Alpha { get; set; } = 0.05d;
    public string TestType { get; set; } = "two-tailed";
}
