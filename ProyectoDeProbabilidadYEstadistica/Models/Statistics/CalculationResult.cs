namespace ProyectoDeProbabilidadYEstadistica.Models;

public class CalculationResult<T>
{
    private CalculationResult(bool succeeded, T? value, IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        Succeeded = succeeded;
        Value = value;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    public static CalculationResult<T> Success(T value)
    {
        return new CalculationResult<T>(true, value, new Dictionary<string, IReadOnlyList<string>>());
    }

    public static CalculationResult<T> Failure(Dictionary<string, List<string>> errors)
    {
        var normalized = errors.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.ToArray());

        return new CalculationResult<T>(false, default, normalized);
    }
}
