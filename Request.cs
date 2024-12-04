using IHttpMachine.Model;

namespace SharpHttpServer;

class Request
{

    public HttpRequestResponse request;

    public Request(HttpRequestResponse r)
    {
        request = r;
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