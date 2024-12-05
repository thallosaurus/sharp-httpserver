using IHttpMachine.Model;

namespace SharpHttpServer;

delegate Response ResponseHandler(Request req, Response res);

/// <summary>
/// Describes the parameters of the middleware interface
/// </summary>
/// <typeparam name="T">Generic for the Request</typeparam>
/// <typeparam name="U">Generic for the Response</typeparam>
/// <param name="req"></param>
/// <param name="res"></param>
/// <returns>A tuple consisting of (T, U)</returns>
delegate (Request, Response) RequestTransformer(Request req, Response res);

class Router
{
    private Dictionary<(string, string), ResponseHandler> routes = new();
    private List<RequestTransformer> middlewares = new();
    static Router router = new();
    public Router() { }
    public static void AddRoute(string path, string method, ResponseHandler fn)
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
                (req, res) = mw.Invoke(req, res);
            }

            // search for the requested route
            var r = (from s in router.routes where s.Key == (req.Method, req.Path) select s).ToList();

            if (r.Count > 0)
            {
                var route = r.First().Value;
                return route.Invoke(req, res);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        // Route was not defined
        catch (KeyNotFoundException knf)
        {
            Console.WriteLine(knf);
            return Response.CreateNotFound();
        }

        // A general exception
        // Respond with 500
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new(500, e.ToString());
        }
    }
}