namespace VRAtlas.Logging;
#pragma warning disable CA2254 // Template should be a static expression

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
}

#pragma warning restore CA2254 // Template should be a static expression