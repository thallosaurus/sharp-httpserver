using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

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
            WorkerMessage msg;
            var buffer = new byte[1024];
            string data;
            // wait until the request channel gets a new request
            await requestChannel.Reader.WaitToReadAsync(tokenSource.Token);
            msg = await requestChannel.Reader.ReadAsync(tokenSource.Token);
            var received = await msg.socket.ReceiveAsync(buffer, SocketFlags.None);
            data = Encoding.UTF8.GetString(buffer, 0, received);

            var req = Parse(data.Split("\r\n"));

            try
            {
                var resp = Router.Exec(req);
                _ = await msg.socket.SendAsync(resp.GetBytes(), 0, tokenSource.Token);
            }
            finally
            {
                msg.socket.Close();
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

    /// <summary>
    /// Converts a Request to object
    /// </summary>
    /// <param name="req"></param>
    /// <returns>Request</returns>
    private Request Parse(IEnumerable<string> req)
    {
        var list = req.ToList();
        var r = parseFirstField(list.First());

        var headers = new Dictionary<string, string>();

        foreach (var item in list.Skip(1))
        {
            if (item.Length != 0)
            {
                var sepIndex = item.IndexOf(":");

                string key = item.Substring(0, sepIndex);
                string value = item.Substring(sepIndex + 1);

                headers.Add(key, value.Trim());
            }
            else
            {
                break;
            }
        }

        /*if (r.method == Method.POST) {
            var postData = list.Last();
        }*/

        return new Request(r, headers);

    }

    private HttpMeta parseFirstField(string field)
    {
        var fields = field.Split(" ");

        var m = new HttpMeta();

        Method method;
        Enum.TryParse(fields[0], out method);

        try
        {
            m.method = method;
            m.path = fields[1];
            m.version = fields[2];
        }
        catch (IndexOutOfRangeException e)
        {
            Logger.Error(e.ToString());
        }

        return m;
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