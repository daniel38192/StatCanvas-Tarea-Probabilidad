namespace ProyectoDeProbabilidadYEstadistica.Models;

public class TStudentInputViewModel
{
    public string RawValues { get; set; } = string.Empty;
    public double? HypothesizedMean { get; set; }
    public double? Alpha { get; set; } = 0.05d;
    public string TestType { get; set; } = "two-tailed";
    public string NullHypothesisText { get; set; } = string.Empty;
    public string AlternativeHypothesisText { get; set; } = string.Empty;
}
