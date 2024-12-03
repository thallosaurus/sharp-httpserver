using System.Net.Sockets;
using System.Threading.Channels;

namespace HttpServer;

class Worker
{
    public int ThreadId { get; }
    private Thread workerThread { get; set; }
    private Channel<WorkerMessage> requestChannel;
    public bool available = false;
    private CancellationTokenSource tokenSource;
    public Worker(int id, CancellationToken cancellationTokenSource)
    {
        ThreadId = id;
        tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource);

        requestChannel = Channel.CreateUnbounded<WorkerMessage>();

        workerThread = new Thread(() => RunWorker());
        workerThread.Start();


        Console.WriteLine($"Spawned Worker Thread {id}");
    }

    void LockWorker() {
        available = false;
    }

    void UnlockWorker() {
        available = true;
    }

    async void RunWorker()
    {
        UnlockWorker();
        do
        {
            // wait until the request channel gets a new request
            if (await requestChannel.Reader.WaitToReadAsync(tokenSource.Token))
            {
                LockWorker();
                // read all request in
                var msg = await requestChannel.Reader.ReadAsync(tokenSource.Token);

                var req = Parse(msg.data.Split("\r\n"));
                //Console.WriteLine($"Thread {ThreadId} got message:\n{msg.data}, {req}");

                var resp = Router.Exec(req);

                var sentBytes = await msg.socket.SendAsync(resp.GetBytes(), 0);
                //Console.WriteLine($"Send {sentBytes} bytes");
                msg.socket.Close();

                UnlockWorker();
            } else {
                break;
            }

        } while (!tokenSource.IsCancellationRequested);
        Console.WriteLine($"Closing Worker {ThreadId}");
    }

    public async void SendRequest(string request, Socket handler)
    {
        // send request to worker
        if (await requestChannel.Writer.WaitToWriteAsync(tokenSource.Token))
        {
            await requestChannel.Writer.WriteAsync(new WorkerMessage(request, handler), tokenSource.Token);
        }
    }

    private Request Parse(IEnumerable<string> req)
    {
        var list = req.ToList();
        var r = parseFirstField(list.First());
        list.RemoveAt(0);

        var headers = new Dictionary<string, string>();

        foreach (var item in list)
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

        var postData = list.Last();

        return new Request(r, headers);

    }

    private HttpMeta parseFirstField(string field)
    {
        var fields = field.Split(" ");

        var m = new HttpMeta();

        Method method;
        Enum.TryParse(fields[0], out method);

        m.method = method;
        m.path = fields[1];
        m.version = fields[2];

        return m;
    }
}

struct WorkerMessage
{
    public WorkerMessage(string _data, Socket _socket)
    {
        data = _data;
        socket = _socket;
    }
    public string data;
    public Socket socket;
}