namespace SharpHttpServer;

class Request
{
    public string? data;
    public HttpMeta meta;
    private Dictionary<string, string> headers;

    public Request(HttpMeta m, Dictionary<string, string> h)
    {
        meta = m;
        headers = h;
    }
}

struct HttpMeta
{
    public Method method;
    public string path;
    public string version;
}

enum Method {
    GET,
    POST
}