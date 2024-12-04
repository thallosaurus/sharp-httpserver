namespace SharpHttpServer;

delegate Response ResponseHandler(Request req, Response res);
delegate Response RequestTransformer(Request req, Response res);

class Router
{
    private Dictionary<(Method, string), ResponseHandler> routes = new();
    private List<RequestTransformer> middlewares = new();
    static Router router = new();
    public Router() { }
    public static void AddRoute(string path, Method method, ResponseHandler fn)
    {
        router.routes.Add((method, path), fn);
    }

    public static void AddMiddleware(RequestTransformer mw)
    {
        router.middlewares.Add(mw);
    }

    public static Response Exec(Request req)
    {
        try
        {
            var res = new Response(200, "");

            foreach (var mw in router.middlewares)
            {
                res = mw.Invoke(req, res);
            }

            var r = (from s in router.routes where s.Key == (req.meta.method, req.meta.path) select s).ToList();
            if (r.Count > 0)
            {
                var route = r.First().Value;
                return route.Invoke(req, res);
            }
            else
            {
                return Response.CreateNotFound();
            }

            //var route = router.routes[(req.meta.method, req.meta.path)];
        }
        catch (KeyNotFoundException)
        {
            return Response.CreateNotFound();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new(500, e.ToString());
        }
    }
}