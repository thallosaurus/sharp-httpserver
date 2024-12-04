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
    public Worker(int id)
    {
        ThreadId = id;
        tokenSource = new CancellationTokenSource();

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

    /// <summary>
    /// Workers Main Function
    /// </summary>
    async void RunWorker()
    {
        Console.WriteLine($"Spawned Worker Thread {ThreadId}");
        UnlockWorker();
        while (!tokenSource.IsCancellationRequested)
        {
            // wait until the request channel gets a new request
            await requestChannel.Reader.WaitToReadAsync(tokenSource.Token);
            LockWorker();

            WorkerMessage msg;
            var buffer = new byte[1024];
            msg = await requestChannel.Reader.ReadAsync(tokenSource.Token);
            _ = await msg.socket.ReceiveAsync(buffer, SocketFlags.None);

            HttpRequestResponse req;
            using (var handler = new HttpParserDelegate())
            using (var parser = new HttpCombinedParser(handler))
            {
                var length = parser.Execute(buffer);
                //Console.WriteLine($"Parsed Request Length: {l}");
                req = handler.HttpRequestResponse;
            }            

            try
            {
                Response resp;
                if (req == null)
                {
                    resp = Response.CreateBadRequest();
                }
                else
                {

                    resp = Router.Exec(req);
                }
                _ = await msg.socket.SendAsync(resp.GetBytes(), 0, tokenSource.Token);
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            finally
            {
                msg.socket.Close();
                UnlockWorker();
            }

        }
        Console.WriteLine($"Closing Worker {ThreadId}");
    }

    /// <summary>
    /// Sends the handler to the worker
    /// </summary>
    /// <param name="handler"></param>
    public async void SendRequest(Socket handler)
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