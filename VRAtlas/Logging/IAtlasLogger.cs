namespace VRAtlas.Logging;

public interface IAtlasLogger
{
    void LogInformation(string? message, params object?[] args);
    void LogCritical(string? message, params object?[] args);
}

public interface IAtlasLogger<T> : IAtlasLogger
{

}