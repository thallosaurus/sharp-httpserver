# sharp-httpserver
A toy HTTP Server intended for learning C#. It exposes a Routing API and a Request Interface similar to ExpressJS.

## Usage
### Even though you shouldn't

```cs
using System.Net;
using HttpServer;

// Supports Middlewares
Router.AddMiddleware((req, res) => {
    Console.WriteLine($"{req.meta.method} {req.meta.path}");
    return res;
});

// Supports Simple routing
Router.AddRoute("/", Method.GET, (req, res) => {
    res.status = 200;
    res.ServeFile("index.html");
    return res;
});

// Even Multithreaded Worker Support
Router.AddRoute("/sleep", Method.GET, (req, res) => {
    Thread.Sleep(5000);
    res.status = 200;
    res.body = "slept for 5s";
    return res;
});

var server = new Server(IPEndPoint.Parse("127.0.0.1:8080"));
await server.Start();
```