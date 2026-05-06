namespace ProyectoDeProbabilidadYEstadistica.Models;

public class StandardNormalInputViewModel
{
    public string FormMode { get; set; } = "normal";
    public string CalculationMode { get; set; } = "from-z";
    public double? ZValue { get; set; }
    public double? AreaValue { get; set; }
    public string AreaSide { get; set; } = "left";
    public string LowerBound { get; set; } = string.Empty;
    public string UpperBound { get; set; } = string.Empty;
}
