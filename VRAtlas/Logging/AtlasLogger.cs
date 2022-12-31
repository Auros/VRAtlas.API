#pragma warning disable CA2254 // Template should be a static expression
namespace VRAtlas.Logging;

public class AtlasLogger<T> : IAtlasLogger<T>
{
	private readonly ILogger<T> _logger;

	public AtlasLogger(ILogger<T> logger)
	{
		_logger = logger;
	}

    public void LogInformation(string? message, params object?[] args)
	{
		_logger.LogInformation(message, args);
	}

    public void LogCritical(string? message, params object?[] args)
	{
        _logger.LogCritical(message, args);
    }

    public void LogWarning(string? message, params object?[] args)
    {
		_logger.LogWarning(message, args);
    }

    public void LogDebug(string? message, params object?[] args)
    {
        _logger.LogDebug(message, args);
    }

    public void LogError(string? message, params object?[] args)
    {
        _logger.LogError(message, args);
    }
}
#pragma warning restore CA2254 // Template should be a static expression