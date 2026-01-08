using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eVerse.Services;

namespace eVerse.ViewModels
{
    public partial class WebsocketConfigViewModel : ObservableObject
    {
        private readonly IWebSocketService _webSocketService;

        public string WebsocketAddress => _webSocketService.Address;
        public string IsRunningText => _webSocketService.IsRunning ? "En ejecucion" : "Detenido";
        public string MdnsPublishedText => _webSocketService.MdnsPublished ? "Si" : "No";
        public string? LocalIp => _webSocketService.LocalIp;

        public WebsocketConfigViewModel(IWebSocketService webSocketService)
        {
            _webSocketService = webSocketService;
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void Start()
        {
            _webSocketService.Start();
            Refresh();
        }

        private bool CanStart() => !_webSocketService.IsRunning;

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            _webSocketService.Stop();
            Refresh();
        }

        private bool CanStop() => _webSocketService.IsRunning;

        [RelayCommand]
        private void CopyAddress()
        {
            try { System.Windows.Clipboard.SetText(WebsocketAddress); } catch { }
        }

        private void Refresh()
        {
            OnPropertyChanged(nameof(IsRunningText));
            OnPropertyChanged(nameof(WebsocketAddress));
            OnPropertyChanged(nameof(MdnsPublishedText));
            OnPropertyChanged(nameof(LocalIp));
            // Notify commands to requery CanExecute
            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
    }
}