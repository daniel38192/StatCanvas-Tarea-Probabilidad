namespace ProyectoDeProbabilidadYEstadistica.Models;

public class NegativeBinomialResultViewModel
{
    public int TrialSize { get; init; }
    public double SuccessProbability { get; init; }
    public double FailureProbability { get; init; }
    public string CalculationMode { get; init; } = "single";
    public int? XValue { get; init; }
    public int? StartX { get; init; }
    public int? EndX { get; init; }
    public string OperationLabel { get; init; } = string.Empty;
    public double Probability { get; init; }
    public IReadOnlyList<NegativeBinomialTermViewModel> Terms { get; init; } = [];
}
