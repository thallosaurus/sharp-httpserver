using System.Text;

namespace SharpHttpServer;

class Response {
    public int status;
    public string? body;

    public Response(int s, string b) {
        status = s;
        body = b;
    }

    public static Response CreateOk() {
        return new(200, "");
    }

    public static Response CreateBadRequest() {
        return new(400, "Bad Request");
    }

    public static Response CreateNotFound() {
        return new(404, "Not found");
    }

    public byte[] GetBytes() {
        var resp = $"HTTP/1.1 {GetStatusAsString(status)}\r\n\r\n{body}\r\n\r\n";
        return Encoding.UTF8.GetBytes(resp);
    }

    static string GetStatusAsString(int status) {
        switch (status) {
            case 200:
                return "200 OK";

            case 404:
                return "404 Not found";
            default:
                return "500 Internal Server Error";
        }
    }

    public Response ServeFile(string path) {

        //TODO The devil lies in the detail
        if (Path.Exists(path)) {
            using (StreamReader sr = File.OpenText(path))
            {
                body = sr.ReadToEnd();
                return this;
            }
        } else {
            return Response.CreateNotFound();
        }
    }
}