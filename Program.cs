using System.Net;
using SharpHttpServer;

Router.AddMiddleware((req, res) => {
    Console.WriteLine($"{req.meta.method} {req.meta.path}");
    return res;
});

Router.AddRoute("/", Method.GET, (req, res) => {
    res.status = 200;
    res.ServeFile("index.html");
    return res;
});

Router.AddRoute("/sleep", Method.GET, (req, res) => {
    Thread.Sleep(5000);
    res.status = 200;
    res.body = "slept for 5s";
    return res;
});

var server = new Server(IPEndPoint.Parse("127.0.0.1:8080"));
//await Task.Run(server.Start);
Thread.Sleep(10000);

server.Stop();
