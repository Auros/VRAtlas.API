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
}