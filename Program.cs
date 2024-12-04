using System.Net;
using IHttpMachine.Model;
using SharpHttpServer;

Router.AddMiddleware((req) => {
    //Console.WriteLine($"{req.Method} {req.Path}");
    return new TestMiddleware(req);
});

Router.AddRoute("/", "GET", (req, res) => {
    res.status = 200;
    res.ServeFile("index.html");
    return res;
});

Router.AddRoute("/msg", "GET", (req, res) => {
    var o = (TestMiddleware) req;
    res.status = 200;
    res.body = o.TestExtension;
    return res;
});

Router.AddRoute("/sleep", "GET", (req, res) => {
    Thread.Sleep(5000);
    res.status = 200;
    res.body = "slept for 5s";
    return res;
});

var server = new Server(IPEndPoint.Parse("127.0.0.1:8080"));
Task.Run(server.Start).Wait();

record TestMiddleware : HttpRequestResponse
{
    public string TestExtension;
    public TestMiddleware(IHttpRequestResponse httpRequestResponse) : base(httpRequestResponse)
    {
        TestExtension = "deine mudda";
    }
}