using eVerse.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace eVerse.ViewModels
{
 public class WebsocketConfigViewModel : INotifyPropertyChanged
 {
 private readonly IWebSocketService _webSocketService;

 public ICommand StartCommand { get; }
 public ICommand StopCommand { get; }
 public ICommand CopyAddressCommand { get; }

 public string WebsocketAddress => _webSocketService.Address;
 public string IsRunningText => _webSocketService.IsRunning ? "En ejecucion" : "Detenido";
 public string MdnsPublishedText => _webSocketService.MdnsPublished ? "Si" : "No";
 public string? LocalIp => _webSocketService.LocalIp;

 public WebsocketConfigViewModel(IWebSocketService webSocketService)
 {
 _webSocketService = webSocketService;
 StartCommand = new RelayCommand(() => { _webSocketService.Start(); Refresh(); });
 StopCommand = new RelayCommand(() => { _webSocketService.Stop(); Refresh(); });
 CopyAddressCommand = new RelayCommand(() =>
 {
 try { System.Windows.Clipboard.SetText(WebsocketAddress); } catch { }
 });
 }

 private void Refresh()
 {
 OnPropertyChanged(nameof(IsRunningText));
 OnPropertyChanged(nameof(WebsocketAddress));
 OnPropertyChanged(nameof(MdnsPublishedText));
 OnPropertyChanged(nameof(LocalIp));
 }

 public event PropertyChangedEventHandler? PropertyChanged;
 private void OnPropertyChanged([CallerMemberName] string? name = null)
 => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
 }
}