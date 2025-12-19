using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace eVerse.Services
{
    public class WebSocketService : IWebSocketService, IDisposable
    {
        private readonly HttpListener _listener;
        private readonly List<WebSocket> _sockets = new();
        private readonly object _lock = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;

        // Configurable
        private readonly int _port;
        private readonly string _path;
        private readonly string _token;

        // Public address exposed for clients
        public string Address { get; }

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public string? LocalIp { get; private set; }
        public bool MdnsPublished { get; private set; }

        public WebSocketService(int port = 5000, string path = "/projection", string token = "secret-token")
        {
            _port = port;
            _path = path.StartsWith("/") ? path : "/" + path;
            _token = token ?? string.Empty;

            // Determine a local non-loopback IP if available, otherwise localhost
            LocalIp = GetLocalIp() ?? "localhost";

            Address = $"ws://{LocalIp}:{_port}{_path}?token={_token}";
            MdnsPublished = false;

            _listener = new HttpListener();
            var prefix = $"http://{LocalIp}:{_port}{_path}/"; // HttpListener requires trailing slash
            _listener.Prefixes.Add(prefix);
        }

        private string? GetLocalIp()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
                return ip?.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebSocketService: Error getting local IP: {ex.Message}");
                return null;
            }
        }

        public void Start()
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoop(_cts.Token));
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                try { _listener.Stop(); } catch { }
            }
            catch { }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            try
            {
                _listener.Start();
                Debug.WriteLine($"WebSocketService listening on {_listener.Prefixes.Cast<string>().FirstOrDefault()}");

                while (!token.IsCancellationRequested)
                {
                    var ctx = await _listener.GetContextAsync().ConfigureAwait(false);

                    // If it's not a websocket request, reject
                    if (!ctx.Request.IsWebSocketRequest)
                    {
                        ctx.Response.StatusCode = 400;
                        ctx.Response.Close();
                        continue;
                    }

                    // Simple token check in query string
                    var tokenq = ctx.Request.QueryString["token"];
                    if (string.IsNullOrEmpty(tokenq) || tokenq != _token)
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.Close();
                        continue;
                    }

                    try
                    {
                        var wsContext = await ctx.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
                        var ws = wsContext.WebSocket;

                        lock (_lock)
                        {
                            _sockets.Add(ws);
                        }

                        Debug.WriteLine("WebSocket client connected.");

                        // Start receive loop to enforce read-only client (if client tries to send, close)
                        _ = Task.Run(() => ReceiveLoop(ws));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"WebSocket accept failed: {ex.Message}");
                        try { ctx.Response.Abort(); } catch { }
                    }
                }
            }
            catch (HttpListenerException hlex)
            {
                Debug.WriteLine($"WebSocketService listener stopped: {hlex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebSocketService exception: {ex.Message}");
            }
        }

        private async Task ReceiveLoop(WebSocket ws)
        {
            var buffer = new byte[1024];
            try
            {
                while (ws.State == WebSocketState.Open && _cts != null && !_cts.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false);
                        break;
                    }

                    // If client sent anything, close connection (read-only server)
                    Debug.WriteLine("WebSocket client attempted to send data; closing connection.");
                    try
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Server is read-only", CancellationToken.None).ConfigureAwait(false);
                    }
                    catch { }
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReceiveLoop error: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _sockets.Remove(ws);
                }
                try { ws.Dispose(); } catch { }
            }
        }

        public async Task BroadcastTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) text = string.Empty;
            var buffer = Encoding.UTF8.GetBytes(text);
            List<WebSocket> toRemove = new();

            lock (_lock)
            {
                // copy to avoid locking during sends
                toRemove = _sockets.Where(s => s.State != WebSocketState.Open).ToList();
            }

            foreach (var s in toRemove)
            {
                lock (_lock) { _sockets.Remove(s); }
            }

            List<Task> tasks = new();
            lock (_lock)
            {
                foreach (var ws in _sockets.ToList())
                {
                    if (ws.State != WebSocketState.Open) continue;
                    try
                    {
                        var task = ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        tasks.Add(task);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Broadcast send error: {ex.Message}");
                        toRemove.Add(ws);
                    }
                }
            }

            if (tasks.Count > 0)
            {
                try { await Task.WhenAll(tasks).ConfigureAwait(false); } catch { }
            }

            lock (_lock)
            {
                foreach (var s in toRemove) _sockets.Remove(s);
            }
        }

        public void BroadcastText(string text)
        {
            _ = BroadcastTextAsync(text);
        }

        public void Dispose()
        {
            try
            {
                _cts?.Cancel();
                try { _listener.Stop(); } catch { }

                lock (_lock)
                {
                    foreach (var ws in _sockets)
                    {
                        try { ws.Abort(); } catch { }
                        try { ws.Dispose(); } catch { }
                    }
                    _sockets.Clear();
                }
            }
            catch { }
        }
    }
}
