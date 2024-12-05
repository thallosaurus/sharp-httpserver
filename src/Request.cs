using HttpMachine;
using IHttpMachine.Model;

namespace SharpHttpServer;

public class Request
{
    private HttpRequestResponse original;
    public string Method;
    public string Path;
    public IDictionary<string, IEnumerable<string>> Headers
    {
        get {
            return original.Headers;
        }
    }

    public Request(IHttpRequestResponse httpRequestResponse)
    {
        original = (HttpRequestResponse)httpRequestResponse;
        Method = original.Method;
        Path = original.Path;
    }

    public static Request ParseRequest(byte[] buffer)
    {
        HttpRequestResponse req;
        using (var handler = new HttpParserDelegate())
        using (var parser = new HttpCombinedParser(handler))
        {
            var length = parser.Execute(buffer);
            //Console.WriteLine($"Parsed Request Length: {l}");
            req = handler.HttpRequestResponse;
        }

        if (req == null)
        {
            throw new InvalidRequest();
        }
        else return new Request(req);
    }
}

class InvalidRequest() : Exception { }