using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharpHttpServer;

class Server
{
    static int WorkerCount = 10;
    private List<Worker> workers;

    private CancellationTokenSource cancellationTokenSource;

    private IPEndPoint ipEndPoint;

    public Server(IPEndPoint iep)
    {
        workers = new();
        cancellationTokenSource = new CancellationTokenSource();

        // Spawn defined worker count
        for (int i = 0; i < WorkerCount; i++)
        {
            // Thread 0 is reserved for Server Thread
            workers.Add(new Worker(i + 1, cancellationTokenSource.Token));
        }

        ipEndPoint = iep;
    }

    public async Task Start()
    {
        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            listener.Bind(ipEndPoint);
            listener.Listen(ipEndPoint.Port);

            do
            {
                var handler = await listener.AcceptAsync(cancellationTokenSource.Token);

                var buffer = new byte[1024];
                var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                var request = Encoding.UTF8.GetString(buffer, 0, received);
                HandleRequest(request, handler);
            } while (!cancellationTokenSource.IsCancellationRequested);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally {
            Stop();
        }
    }

    private void HandleRequest(string request, Socket handler)
    {
        // Get the first available Worker
        var worker = GetAvailableWorkers().First();

        // Send the Request over to the worker
        Task.Run(() => worker.SendRequest(request, handler));
    }

    public void Stop()
    {
        cancellationTokenSource.Cancel();
    }

    private IEnumerable<Worker> GetAvailableWorkers()
    {
        return from worker in workers
               where worker.available
               select worker;
    }
}