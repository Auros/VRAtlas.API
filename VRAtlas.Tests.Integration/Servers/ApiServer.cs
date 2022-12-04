using WireMock.Server;

namespace VRAtlas.Tests.Integration.Servers;

internal abstract class ApiServer : IDisposable
{
    private string? _url;
    private WireMockServer? _server;

    public string Url
    {
        get
        {
            if (_url is null)
                throw new InvalidOperationException($"{GetType().Name} must be started using .Start() before accessing the url");
            return _url;
        }
    }

    protected WireMockServer Server
    {
        get
        {
            if (_server is null)
                throw new InvalidOperationException("ApiServer must be started using .Start() before setting up.");
            return _server;
        }
    }

    public void Start()
    {
        _server = WireMockServer.Start();
        _url = _server.Url;
    }

    public void Dispose()
    {
        _server?.Stop();
        _server?.Dispose();
    }
}