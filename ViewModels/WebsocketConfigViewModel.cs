using eVerse.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace eVerse.ViewModels
{
 public class WebsocketConfigViewModel : INotifyPropertyChanged
 {
 private readonly IWebSocketService _webSocketService;

 public ICommand StartCommand { get; }
 public ICommand StopCommand { get; }

 public string WebsocketAddress => _webSocketService.Address;
 public string IsRunningText => _webSocketService.IsRunning ? "En ejecución" : "Detenido";

 public WebsocketConfigViewModel(IWebSocketService webSocketService)
 {
 _webSocketService = webSocketService;
 StartCommand = new RelayCommand(() => { _webSocketService.Start(); OnPropertyChanged(nameof(IsRunningText)); });
 StopCommand = new RelayCommand(() => { _webSocketService.Stop(); OnPropertyChanged(nameof(IsRunningText)); });
 }

 public event PropertyChangedEventHandler? PropertyChanged;
 private void OnPropertyChanged([CallerMemberName] string? name = null)
 => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
 }
}