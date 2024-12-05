using System.Text;
using HttpMachine;
using IHttpMachine.Model;

namespace SharpHttpServer;

class Response
{
    public int status;
    public string? body;
    public Dictionary<string, string> Headers = new();

    public Response(int s, string b)
    {
        status = s;
        body = b;
        Headers.Add("Server", "sharp-httpserver");
    }

    public static Response CreateOk()
    {
        return new(200, "");
    }

    public static Response CreateBadRequest()
    {
        return new(400, "Bad Request");
    }

    public static Response CreateNotFound()
    {
        return new(404, "Not found");
    }

    private string GetHeadersAsString() {      
        StringBuilder sb = new();
        foreach (var he in Headers.Select(x => $"{x.Key}: {x.Value}")) {
            sb.Append(he + "\r\n");
        }

        return sb.ToString();
    }

    public byte[] GetBytes()
    {
        int bodyLength = 0;
        if (body != null) {
            bodyLength = body.Length;
        }
        
        Headers.Add("Content-Length", bodyLength.ToString());

        var resp = $"HTTP/1.1 {GetStatusAsString(status)}\r\n{GetHeadersAsString()}\r\n{body}";
        return Encoding.UTF8.GetBytes(resp);
    }

    static string GetStatusAsString(int status)
    {
        switch (status)
        {
            case 200:
                return "200 OK";

            case 404:
                return "404 Not found";

            default:
                return "500 Internal Server Error";
        }
    }

    public Response ServeFile(string path)
    {

        //TODO The devil lies in the detail
        if (Path.Exists(path))
        {
            using (StreamReader sr = File.OpenText(path))
            {
                body = sr.ReadToEnd();
                return this;
            }
        }
        else
        {
            return CreateNotFound();
        }
    }
}