using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Makaretu.Dns;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace eVerse.Services
{
    // Kestrel-based WebSocket server with mDNS advertisement using Makaretu.Dns
    public class KestrelWebSocketService : IWebSocketService, IDisposable
    {
        private IHost? _host;
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();

        // mDNS
        private MulticastService? _multicast;
        private ServiceDiscovery? _sd;

        private readonly int _port;
        private readonly string _token;
        private readonly string _hostname = "iellaurel"; // instance name
        private readonly string _domain = "local"; // standard mDNS domain

        public string Address { get; private set; }
        public string? LocalIp { get; private set; }
        public bool MdnsPublished { get; private set; }

        public bool IsRunning => _host != null;

        public KestrelWebSocketService(int port = 5000, string token = "secret-token")
        {
            _port = port;
            _token = token;
            // Determine local IP
            LocalIp = GetLocalIp();
            // Expose the mDNS name as the canonical address and fallback to ip
            Address = LocalIp != null ? $"http://{_hostname}.{_domain}:{_port} (http://{LocalIp}:{_port})" : $"http://{_hostname}.{_domain}:{_port}";
            MdnsPublished = false;
        }

        private string? GetLocalIp()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
                return ip?.ToString();
            }
            catch { return null; }
        }

        public void Start()
        {
            if (IsRunning) return;

            System.Diagnostics.Debug.WriteLine("KestrelWebSocketService: Starting server...");
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(_port);
            });

            var webRoot = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            if (!System.IO.Directory.Exists(webRoot))
                System.IO.Directory.CreateDirectory(webRoot);

            // Ensure default files are present so static file middleware can serve them (useful when running from bin)
            EnsureDefaultFiles(webRoot);

            var app = builder.Build();

            app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(webRoot) });

            var wsOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            };
            app.UseWebSockets(wsOptions);

            app.MapGet("/", (HttpContext context) =>
            {
                context.Response.Redirect("/index.html");
                return Task.CompletedTask;
            });

            app.MapGet("/status", (HttpContext context) =>
            {
                var status = new {
                    running = IsRunning,
                    mdns = MdnsPublished,
                    address = Address,
                    localIp = LocalIp,
                    port = _port
                };
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(JsonSerializer.Serialize(status));
            });

            app.MapGet("/ws", async (HttpContext context) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                var token = context.Request.Query["token"].ToString();
                if (string.IsNullOrEmpty(token) || token != _token)
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                var ws = await context.WebSockets.AcceptWebSocketAsync();
                var id = Guid.NewGuid();
                _sockets[id] = ws;

                var buffer = new byte[1024];
                try
                {
                    while (ws.State == WebSocketState.Open)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            break;
                        }

                        // client sent data -> policy violation
                        await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Read-only server", CancellationToken.None);
                        break;
                    }
                }
                catch { }
                finally
                {
                    _sockets.TryRemove(id, out _);
                    try { ws.Dispose(); } catch { }
                }
            });

            // Start the web app and keep a reference to the IHost
            _host = app;
            _ = app.StartAsync();

            // Start mDNS advertisement using Makaretu.Dns
            TryStartMdns();
        }

        private void TryStartMdns()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("KestrelWebSocketService: Attempting to start mDNS advertisement...");
                _multicast = new MulticastService();
                _sd = new ServiceDiscovery(_multicast);
                // Advertise an HTTP service under the chosen hostname (iellaurel.local)
                var profile = new ServiceProfile(_hostname, "_http._tcp", (ushort)_port);
                // Try to set HostName via reflection if property exists
                try
                {
                    var hostProp = profile.GetType().GetProperty("HostName");
                    if (hostProp != null && hostProp.CanWrite)
                    {
                        hostProp.SetValue(profile, _hostname + ".local");
                    }
                }
                catch { }
                _sd.Advertise(profile);
                _multicast.Start();
                MdnsPublished = true;
                System.Diagnostics.Debug.WriteLine("KestrelWebSocketService: mDNS advertised as " + _hostname + "." + _domain + ":" + _port);

                // Additional diagnostics: list network interfaces and IPv4 addresses
                try
                {
                    var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                        .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up);
                    foreach (var nic in nics)
                    {
                        var addrs = nic.GetIPProperties().UnicastAddresses
                            .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .Select(a => a.Address.ToString());
                        System.Diagnostics.Debug.WriteLine($"Interface: {nic.Name} - {nic.Description} - IPs: {string.Join(",", addrs)}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to enumerate network interfaces: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"mDNS advertise failed: {ex.Message}");
                MdnsPublished = false;
            }
        }

        private void TryStopMdns()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("KestrelWebSocketService: Stopping mDNS advertisement...");
                if (_sd != null)
                {
                    _sd.Dispose();
                    _sd = null;
                }
                if (_multicast != null)
                {
                    _multicast.Stop();
                    _multicast.Dispose();
                    _multicast = null;
                }
                MdnsPublished = false;
                System.Diagnostics.Debug.WriteLine("KestrelWebSocketService: mDNS stopped");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"TryStopMdns failed: {ex.Message}"); MdnsPublished = false; }
        }

        public void Stop()
        {
            System.Diagnostics.Debug.WriteLine("KestrelWebSocketService: Stopping server...");
            // Stop mDNS
            TryStopMdns();

            try
            {
                foreach (var ws in _sockets.Values.ToList())
                {
                    try { ws.Abort(); } catch { }
                }
                _sockets.Clear();
            }
            catch { }

            try
            {
                if (_host != null)
                {
                    var hostToStop = _host;
                    _host = null;
                    Task.Run(async () =>
                    {
                        try { await hostToStop.StopAsync().ConfigureAwait(false); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"StopAsync failed: {ex.Message}"); }
                        try { hostToStop.Dispose(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Dispose host failed: {ex.Message}"); }
                    });
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Stop failed: {ex.Message}"); }
        }

        public void BroadcastText(string text)
        {
            _ = BroadcastTextAsync(text);
        }

        public async Task BroadcastTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) text = string.Empty;
            var buffer = Encoding.UTF8.GetBytes(text);
            var tasks = new List<Task>();

            foreach (var kv in _sockets.ToArray())
            {
                var ws = kv.Value;
                if (ws.State != WebSocketState.Open) continue;
                try
                {
                    tasks.Add(ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None));
                }
                catch { }
            }

            if (tasks.Count > 0)
            {
                try { await Task.WhenAll(tasks).ConfigureAwait(false); } catch { }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void EnsureDefaultFiles(string webRoot)
        {
            try
            {
                var indexPath = System.IO.Path.Combine(webRoot, "index.html");
                var jsFolder = System.IO.Path.Combine(webRoot, "js");
                var jsPath = System.IO.Path.Combine(jsFolder, "ws-client.js");

                if (!System.IO.Directory.Exists(jsFolder))
                    System.IO.Directory.CreateDirectory(jsFolder);

                // Write index.html if missing
                if (!System.IO.File.Exists(indexPath))
                {
                    var indexContent = @"<!doctype html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>eVerse Projection Client</title>
  <style>
    body { font-family: Arial, sans-serif; padding:20px; }
    #output { white-space: pre-wrap; border:1px solid #ccc; padding:10px; min-height:200px; }
    #status { margin-bottom:10px; }
  </style>
</head>
<body>
  <h1>eVerse Projection Client</h1>
  <div id=""status"">Estado: <span id=""conn-status"">Desconectado</span></div>
  <div id=""output"">Esperando mensajes...</div>

  <script src=""/js/ws-client.js""></script>
</body>
</html>";
                    System.IO.File.WriteAllText(indexPath, indexContent, System.Text.Encoding.UTF8);
                }

                // Write JS if missing
                if (!System.IO.File.Exists(jsPath))
                {
                    var jsContent = @"(function(){
  const statusEl = document.getElementById('conn-status');
  const outputEl = document.getElementById('output');
  function log(msg){ if(!outputEl) return; outputEl.textContent = msg + '\n' + outputEl.textContent; }
  const token = 'secret-token';
  const wsUrl = (function(){ const host = window.location.hostname; const port = window.location.port || '5000'; return `ws://${host}:${port}/ws?token=${token}`; })();
  let ws;
  function connect(){ statusEl.textContent = 'Conectando...'; ws = new WebSocket(wsUrl); ws.onopen = ()=>{ statusEl.textContent = 'Conectado'; log('Conectado a '+wsUrl); }; ws.onmessage = (ev)=>{ statusEl.textContent = 'Recibiendo'; log(ev.data); }; ws.onclose = ()=>{ statusEl.textContent = 'Desconectado'; log('Desconectado'); setTimeout(connect,3000); }; ws.onerror = (e)=>{ statusEl.textContent = 'Error'; log('Error socket'); }; }
  connect();
})();";
                    System.IO.File.WriteAllText(jsPath, jsContent, System.Text.Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureDefaultFiles failed: {ex.Message}");
            }
        }
    }
}
