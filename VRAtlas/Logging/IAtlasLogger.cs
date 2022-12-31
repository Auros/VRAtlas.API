namespace VRAtlas.Logging;

public interface IAtlasLogger
{
    void LogDebug(string? message, params object?[] args);
    void LogInformation(string? message, params object?[] args);
    void LogCritical(string? message, params object?[] args);
    void LogWarning(string? message, params object?[] args);
    void LogError(string? message, params object?[] args);
}

public interface IAtlasLogger<T> : IAtlasLogger
{

}