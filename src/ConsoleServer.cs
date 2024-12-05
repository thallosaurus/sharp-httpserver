using System.Net;
using IHttpMachine.Model;
using SharpHttpServer;

Router.AddMiddleware((req, res) =>
{
    Console.WriteLine($"{req.Method} {req.Path}");
    return (req, res);
});

Router.AddMiddleware((req, res) =>
{
    //Console.WriteLine($"{req.Method} {req.Path}");
    return (req, res);
});

Router.AddRoute("/", "GET", (req, res) =>
{
    res.status = 200;
    res.ServeFile("index.html");
    return res;
});

Router.AddRoute("/msg", "GET", (req, res) =>
{
    var o = req;
    res.status = 200;
    //res.body = o.TestExtension;
    return res;
});

Router.AddRoute("/sleep", "GET", (req, res) =>
{
    Thread.Sleep(5000);
    res.status = 200;
    res.body = "slept for 5s";
    return res;
});

var server = new ConsoleServer(IPEndPoint.Parse("127.0.0.1:8080"));
Task.Run(server.Start).Wait();

//Thread.Sleep(10000);
//server.Stop();
//Thread.Sleep(1000);


class ConsoleServer : Server
{
    public ConsoleServer(IPEndPoint iep) : base(iep)
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, args) =>
        {
            args.Cancel = true;
            Console.WriteLine($"{sender} Bye");
            Stop();
        });
    }
}