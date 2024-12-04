using IHttpMachine.Model;

namespace SharpHttpServer;

delegate Response ResponseHandler(HttpRequestResponse req, Response res);
delegate T RequestTransformer<T>(T req) where T : HttpRequestResponse;

class Router
{
    private Dictionary<(string, string), ResponseHandler> routes = new();
    private List<RequestTransformer<HttpRequestResponse>> middlewares = new();
    static Router router = new();
    public Router() { }
    public static void AddRoute(string path, string method, ResponseHandler fn)
    {
        router.routes.Add((method, path), fn);
    }

    public static void AddMiddleware(RequestTransformer<HttpRequestResponse> mw)
    {
        router.middlewares.Add(mw);
    }

    public static Response Exec(HttpRequestResponse req)
    {
        try
        {
            var res = new Response(200, "");

            foreach (var mw in router.middlewares)
            {
                req = mw.Invoke(req);
            }

            var r = (from s in router.routes where s.Key == (req.Method, req.Path) select s).ToList();

            if (r.Count > 0)
            {
                var route = r.First().Value;
                return route.Invoke(req, res);
            }
            else
            {
                //return Response.CreateNotFound();
                throw new KeyNotFoundException();
            }
        }
        catch (KeyNotFoundException knf)
        {
            Console.WriteLine(knf);
            return Response.CreateNotFound();
        }
        catch (NullReferenceException nre)
        {
            Console.WriteLine(nre);
            return Response.CreateBadRequest();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new(500, e.ToString());
        }
    }
}