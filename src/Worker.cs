using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using HttpMachine;
using IHttpMachine.Model;

namespace SharpHttpServer;

/// <summary>
/// The Worker Class performs the Work to be done on Requests.
/// 
/// Runs as a daemon in the background
/// </summary>
class Worker
{
    public int ThreadId { get; }
    private Thread workerThread { get; set; }
    private Channel<WorkerMessage> requestChannel;
    public bool available = false;
    private CancellationTokenSource tokenSource;

    /// <summary>
    /// Constructs a new Worker Thread and starts it
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationTokenSource"></param>
    public Worker(int id, CancellationToken token)
    {
        ThreadId = id;
        tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        requestChannel = Channel.CreateUnbounded<WorkerMessage>();

        workerThread = new Thread(RunWorker);
        workerThread.Start();
    }

    void LockWorker()
    {
        available = false;
    }

    void UnlockWorker()
    {
        available = true;
    }

    public Task WaitUntilWorkerExit()
    {
        return Task.Run(async () =>
        {
            var alive = workerThread.IsAlive;

            while (alive)
            {
                await Task.Delay(100);
                alive = workerThread.IsAlive;
            }
        });
    }

    /// <summary>
    /// Workers Main Function
    /// </summary>
    async void RunWorker()
    {
        Console.WriteLine($"Spawned Worker Thread {ThreadId}");
        UnlockWorker();
        try
        {
            while (!tokenSource.IsCancellationRequested)
            {

                // wait until the request channel gets a new request
                await requestChannel.Reader.WaitToReadAsync(tokenSource.Token);
                LockWorker();

                // parse request
                var buffer = new byte[1024];
                WorkerMessage msg = await requestChannel.Reader.ReadAsync(tokenSource.Token);
                _ = await msg.socket.ReceiveAsync(buffer, SocketFlags.None);

                Response resp;
                try
                {
                    var req = Request.ParseRequest(buffer);
                    resp = Router.Exec(req);
                    _ = await msg.socket.SendAsync(resp.GetBytes(), 0, tokenSource.Token);

                    // Close the socket
                    msg.socket.Close();
                }
                catch (InvalidRequest)
                {
                    // Request was invalid, send a 400 back
                    resp = Response.CreateBadRequest();
                }
                catch (SocketException se) {
                    Console.WriteLine(se);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    UnlockWorker();
                }

            }

        }
        catch (OperationCanceledException)
        {
            // cleanup anything here
            Stop();
        }
    }

    /// <summary>
    /// Sends the handler to the worker
    /// </summary>
    /// <param name="handler"></param>
    public async void Handle(Socket handler)
    {
        // send request to worker
        try
        {
            if (await requestChannel.Writer.WaitToWriteAsync(tokenSource.Token))
            {
                await requestChannel.Writer.WriteAsync(new WorkerMessage(handler), tokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    private void Stop()
    {
        if (!tokenSource.IsCancellationRequested)
        {
            tokenSource.Cancel();
        }
        Console.WriteLine($"Closing Worker {ThreadId}");

        LockWorker();
    }
}

struct WorkerMessage
{
    public WorkerMessage(Socket _socket)
    {
        socket = _socket;
    }
    //public string data;
    public Socket socket;
}