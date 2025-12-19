using System.Threading.Tasks;

namespace eVerse.Services
{
 public interface IWebSocketService
 {
 string Address { get; }
 void BroadcastText(string text);
 void Start();
 void Stop();
 bool IsRunning { get; }
 }
}