using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eVerse.Services;
using eVerse.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace eVerse.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IServiceProvider _service;
        private readonly ProjectionService _projectionService;

        public ICommand ShowCreateSongCommand { get; }
        public ICommand ShowSongListCommand { get; }
        public ICommand ShowEditSongListCommand { get; }
        public ICommand ShowWebsocketConfigCommand { get; }

        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public MainWindowViewModel(IServiceProvider service, ProjectionService projectionService)
        {
            _service = service;
            _projectionService = projectionService;

            ShowCreateSongCommand = new RelayCommand(ShowCreateSong);
            ShowSongListCommand = new RelayCommand(ShowSongList);
            ShowEditSongListCommand = new RelayCommand(ShowEditSongList);
            ShowWebsocketConfigCommand = new RelayCommand(ShowWebsocketConfig);

            // Vista inicial
            ShowCreateSong();
        }

        private void ShowCreateSong()
        {
            CurrentView = _service.GetRequiredService<CreateSongView>();
        }

        private void ShowSongList()
        {
            CurrentView = _service.GetRequiredService<SongListView>();
        }

        private void ShowEditSongList()
        {
            CurrentView = _service.GetRequiredService<EditSongListView>();
        }

        private void ShowWebsocketConfig()
        {
            CurrentView = _service.GetRequiredService<WebsocketConfigView>();
        }


        [RelayCommand]
        private void CloseProjection()
        {
            _projectionService.CloseProjection();
        }
    }
}
