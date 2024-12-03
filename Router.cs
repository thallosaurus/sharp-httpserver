namespace SharpHttpServer;

delegate Response ResponseHandler(Request req, Response res);
delegate Response MiddlewareHandler(Request req, Response res);

class Router {
    private Dictionary<(Method, string), ResponseHandler> routes = new();
    private List<MiddlewareHandler> middlewares = new();
    static Router router = new();
    public Router() {}
    public static void AddRoute(string path, Method method, ResponseHandler fn) {
        router.routes.Add((method, path), fn);
    }

    public static void AddMiddleware(MiddlewareHandler mw) {
        router.middlewares.Add(mw);
    }

    public static Response? Exec(Request req) {
        try {
            var res = new Response(200, "");
            
            foreach (var mw in router.middlewares) {
                res = mw.Invoke(req, res);
            }

            var route = router.routes[(req.meta.method, req.meta.path)];
            return route.Invoke(req, res);
        } catch (KeyNotFoundException knfe) {
            //Console.WriteLine(knfe);
            return Response.CreateNotFound();
        } catch (Exception e) {
            Console.WriteLine(e);
            return new(500, e.ToString());
        }
    }
}