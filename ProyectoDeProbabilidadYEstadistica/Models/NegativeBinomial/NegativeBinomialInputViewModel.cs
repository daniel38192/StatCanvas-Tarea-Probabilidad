namespace ProyectoDeProbabilidadYEstadistica.Models;

public class NegativeBinomialInputViewModel
{
    public int? TrialSize { get; set; }
    public double? SuccessProbability { get; set; }
    public string CalculationMode { get; set; } = "single";
    public int? XValue { get; set; }
    public int? IntervalStart { get; set; }
    public int? IntervalEnd { get; set; }
    public int? SumStart { get; set; }
    public int? SumEnd { get; set; }
}
