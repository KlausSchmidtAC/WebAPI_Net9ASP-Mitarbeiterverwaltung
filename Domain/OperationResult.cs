namespace Domain;

/// <summary>
/// Wrapper für Operation-Ergebnisse mit ErrorMessage-Support
/// </summary>
public class OperationResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    public OperationResult(bool success, string? errorMessage = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Erstellt erfolgreiches Ergebnis
    /// </summary>
    public static OperationResult SuccessResult() => new(true);

    /// <summary>
    /// Erstellt fehlerhaftes Ergebnis mit ErrorMessage
    /// </summary>
    public static OperationResult FailureResult(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Generische Version für Operationen mit Rückgabewert
/// </summary>
public class OperationResult<T> : OperationResult
{
    public T? Data { get; private set; }

    private OperationResult(bool success, T? data = default, string? errorMessage = null) 
        : base(success, errorMessage)
    {
        Data = data;
    }

    /// <summary>
    /// Erstellt erfolgreiches Ergebnis mit Daten
    /// </summary>
    public static OperationResult<T> SuccessResult(T data) => new(true, data);

    /// <summary>
    /// Erstellt fehlerhaftes Ergebnis
    /// </summary>
    public static new OperationResult<T> FailureResult(string errorMessage) => new(false, default, errorMessage);
}