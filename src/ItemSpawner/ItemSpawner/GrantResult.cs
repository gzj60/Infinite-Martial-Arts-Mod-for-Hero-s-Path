namespace ItemSpawner;

public readonly struct GrantResult
{
    public bool Success { get; }
    public string Message { get; }

    public GrantResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}
