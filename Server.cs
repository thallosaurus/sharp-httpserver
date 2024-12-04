using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharpHttpServer;

/// <summary>
/// Represents the Server as a whole.
/// Takes requests and sends them to an available worker thread
/// </summary>
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
            workers.Add(new Worker(i + 1));
        }

        ipEndPoint = iep;
    }

    public async Task Start()
    {
        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(ipEndPoint);
        listener.Listen(ipEndPoint.Port);

        do
        {
            Socket handler;
            try
            {
                handler = await listener.AcceptAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            HandleRequest(handler);

        } while (!cancellationTokenSource.IsCancellationRequested);
    }

    /// <summary>
    /// Sends a request to a Worker
    /// </summary>
    /// <param name="handler"></param>
    private void HandleRequest(Socket handler)
    {
        Task.Run(async () =>
        {
            var worker = GetAvailableWorkers();
            while (worker.Count() == 0)
            {
                await Task.Delay(25);
                worker = GetAvailableWorkers();
            }
            worker.First().SendRequest(handler);
        });

        // Get the first available Worker
        // Send the Request over to the worker
    }

    /// <summary>
    /// Stops the Server and all of its worker threads
    /// </summary>
    public void Stop()
    {
        cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Queries the Worker Dict if there are worker available
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Worker> GetAvailableWorkers()
    {
        return from worker in workers
               where worker.available
               select worker;
    }
}