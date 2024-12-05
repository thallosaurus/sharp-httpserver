using System.Net;
using System.Net.Sockets;

namespace SharpHttpServer;

/// <summary>
/// Represents the Server as a whole.
/// Takes requests and sends them to an available worker thread
/// </summary>
class Server
{
    /// <summary>
    /// Controls how many Worker Threads are spawned
    /// </summary>
    static int WorkerCount = 10;
    private List<Worker> workers = new();

    /// <summary>
    /// Cancellation token to cancel all async methods
    /// </summary>
    private CancellationTokenSource cancellationTokenSource = new();

    /// <summary>
    /// The Interface Endpoint the Server should listen to
    /// </summary>
    private IPEndPoint ipEndPoint;

    public Server(IPEndPoint iep)
    {
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
        listener.Bind(ipEndPoint);
        listener.Listen(ipEndPoint.Port);

        // Loop listen for a new connection and handling
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
            // Gets all available Workers
            var worker = GetAvailableWorkers();

            // Blocks until the available worker count is higher than 0
            while (worker.Count() == 0)
            {
                await Task.Delay(25);
                worker = GetAvailableWorkers();
            }

            // Send the Request over to the worker
            worker.First().Handle(handler);
        });
    }

    /// <summary>
    /// Stops the Server and all of its worker threads
    /// </summary>
    public void Stop()
    {
        cancellationTokenSource.Cancel();

        List<Task> tasks = new();

        foreach (var worker in workers)
        {
            tasks.Add(worker.WaitUntilWorkerExit());
        }

        // wait until every worker exited
        Task.WaitAll(tasks.ToArray());
    }

    /// <summary>
    /// Queries the Worker Dict for all available worker
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Worker> GetAvailableWorkers()
    {
        return from worker in workers
               where worker.available
               select worker;
    }
}

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