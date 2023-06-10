using CC_ASP.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CC_ASP.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		public static List<StorageViewModel> storages = new List<StorageViewModel>();

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{
			return View(storages);
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
			var buffer = new byte[1024 * 4];
			var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			_logger.Log(LogLevel.Information, "Message received from Client");

			try
			{
				string s = UTF8Encoding.UTF8.GetString(buffer, 0, result.Count);

				if (s != null)
				{
					string PCname = s.Substring(0, s.IndexOf(','));

					string idk = s.Substring(s.IndexOf(',') + 1);
					_logger.LogInformation(idk);
					List<StorageViewModel> list = JsonSerializer.Deserialize<List<StorageViewModel>>(idk)!;

					_logger.LogInformation("JSONs converted");

					for (int i = 0; i < storages.Count; i++)
					{
						if (storages[i].name == PCname)
							storages.Remove(storages[i]);
					}

					int total = 0;
					foreach (StorageViewModel svm in list)
					{
						total += svm.count;
					}

					storages.Add(new StorageViewModel() { name = PCname, count = total });

					await webSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, result.CloseStatusDescription, CancellationToken.None);
					_logger.Log(LogLevel.Information, "WebSocket connection closed");
				}
			}
			catch
			{
				string s = UTF8Encoding.UTF8.GetString(buffer, 0, result.Count);

				if (s != null)
				{
					string PCname = s.Substring(0, s.IndexOf(','));
					_logger.LogInformation(s.Substring(s.IndexOf(',') + 1));
					List<StorageViewModel> list = JsonSerializer.Deserialize<List<StorageViewModel>>(s.Substring(s.IndexOf(',') + 1))!;

					for (int i = 0; i < storages.Count; i++)
					{
						if (storages[i].name == PCname)
							storages.Remove(storages[i]);
					}

					int total = 0;
					foreach (StorageViewModel svm in list)
					{
						total += svm.count;
					}

					storages.Add(new StorageViewModel() { name = PCname, count = total });
				}
			}
		}
	}
}