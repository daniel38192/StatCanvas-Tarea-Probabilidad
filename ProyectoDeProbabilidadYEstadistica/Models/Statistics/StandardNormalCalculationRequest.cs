namespace ProyectoDeProbabilidadYEstadistica.Models;

public class StandardNormalCalculationRequest
{
    public string FormMode { get; set; } = "normal";
    public string CalculationMode { get; set; } = "from-z";
    public double? ZValue { get; set; }
    public double? AreaValue { get; set; }
    public string AreaSide { get; set; } = "left";
    public string IntervalRange { get; set; } = string.Empty;
}
