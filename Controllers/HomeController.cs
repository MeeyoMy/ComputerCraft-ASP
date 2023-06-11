using CC_ASP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CC_ASP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public static List<StorageViewModel> storages = new List<StorageViewModel>();
        public static List<ItemCountClass> itemCounts = new List<ItemCountClass>();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(storages);
        }

        public IActionResult Detail(string? ComputerName)
        {
            if (ComputerName == null)
            {
                return NotFound();
            }

            List<ItemCountClass> ic = new List<ItemCountClass>();
            ic = itemCounts.FindAll(x => x.ComputerName == ComputerName);

            if (ic == null)
            {
                return NotFound();
            }

            return View(ic);
        }

        [Route("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _logger.Log(LogLevel.Information, "WebSocket connection established");
                await Echo(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        public async Task Echo(WebSocket webSocket)
        {
            int bufferSize = 1024*4;
            var buffer = new byte[bufferSize];
            var offset = 0;
            var free = buffer.Length;
            WebSocketReceiveResult result;

            while (true)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, free), CancellationToken.None);
                _logger.Log(LogLevel.Information, "Message received from Client");
                offset += result.Count;
                free -= result.Count;
                if (result.EndOfMessage) break;
                if (free == 0)
                {
                    var newSize = buffer.Length + bufferSize;
                    var newBuffer = new byte[newSize];
                    Array.Copy(buffer, 0, newBuffer, 0, offset);
                    buffer = newBuffer;
                    free = buffer.Length - offset;
                }
            }

            try
            {
                ConvertWebSocketData(buffer, offset);

                await webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, result.CloseStatusDescription, CancellationToken.None);
                _logger.Log(LogLevel.Information, "WebSocket connection closed");
            }
            catch
            {
                ConvertWebSocketData(buffer, offset);
            }

            storages.Sort(delegate (StorageViewModel s1, StorageViewModel s2) { return s1.count.CompareTo(s2.count) * -1; });
        }

        private void ConvertWebSocketData(byte[] buffer, int offset)
        {
            string s = UTF8Encoding.UTF8.GetString(buffer, 0, offset);
            if (s != null)
            {
                string PCname = s.Substring(0, s.IndexOf(','));
                string jsonString = s.Substring(s.IndexOf(',') + 1);

                List<Deserialised> listTemp = JsonSerializer.Deserialize<List<Deserialised>>(jsonString)!;
                List<string> items = new List<string>();

                List<ItemCountClass> list = new List<ItemCountClass>();

                foreach(Deserialised d in listTemp)
                {
                    list.Add(new ItemCountClass() { ComputerName = PCname, name = d.name, count = d.count });
                }

                for (int i = 0; i < storages.Count; i++)
                {
                    if (storages[i].name == PCname)
                        storages.Remove(storages[i]);
                }

                int total = 0;
                foreach (ItemCountClass svm in list)
                {
                    total += svm.count;
                }

                storages.Add(new StorageViewModel() { name = PCname, count = total });



                foreach (ItemCountClass i in list)
                {
                    bool isInList = false;
                    foreach (string str in items)
                    {
                        if (i.name == str)
                        {
                            isInList = true;
                        }
                    }
                    if (!isInList)
                    {
                        items.Add(i.name);
                    }
                }

                for (int i = 0; i < itemCounts.Count; i++)
                {
                    foreach (string str in items)
                    {
                        if (str == itemCounts[i].name) itemCounts.Remove(itemCounts[i]);
                    }
                }

                int[] totalItems = new int[items.Count];

                for (int i = 0; i < items.Count; i++)
                {
                    foreach (ItemCountClass icc in list)
                    {
                        if (icc.name == items[i])
                        {
                            totalItems[i] += icc.count;
                        }
                    }
                }

                for (int i = 0; i < items.Count; i++)
                {
                    itemCounts.Add(new ItemCountClass() { ComputerName = PCname, name = items[i], count = totalItems[i] });
                }
            }
        }
    }
    class Deserialised
    {
        public string name { get; set; }
        public int count { get; set; }
    }
}