using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eVerse.Services;
using eVerse.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;
using eVerse.Data;
using eVerse.Models;

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

        // Active book indicator
        [ObservableProperty]
        private string activeBookTitle = string.Empty;

        [ObservableProperty]
        private bool isBookSelected = false;

        // Window title that includes active book
        [ObservableProperty]
        private string windowTitle = "eVerse";

        partial void OnActiveBookTitleChanged(string oldValue, string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                WindowTitle = "eVerse";
            else
                WindowTitle = $"eVerse - {newValue}";
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

            // Initialize active book title from AppConfig if present
            try
            {
                using var ctx = new AppDbContext();
                var cfg = ctx.AppConfigs.FirstOrDefault();
                if (cfg != null && cfg.LastOpenedBook > 0)
                {
                    var book = ctx.Books.Find(cfg.LastOpenedBook);
                    if (book != null)
                    {
                        ActiveBookTitle = book.Title;
                        IsBookSelected = true;
                    // Notify CreateSongViewModel to recalculate next song number if it's the current view's VM
                    try
                    {
                        if (CurrentView is CreateSongView csv && csv.DataContext is CreateSongViewModel cvm)
                        {
                            cvm.RecalculateNextSongNumber();
                        }
                    }
                    catch { }
                    }
                }
            }
            catch { }
        }

        private void ShowCreateSong()
        {
            CurrentView = _service.GetRequiredService<CreateSongView>();
        }

        private void ShowSongList()
        {
            CurrentView = _service.GetRequiredService<SongListView>();
        }

        public void LoadSongsForBook(int bookId)
        {
            SongListView targetView;

            if (CurrentView is SongListView existingView && existingView.DataContext is SongListViewModel existingVm)
            {
                targetView = existingView;
                existingVm.LoadSongsByBook(bookId);
            }
            else
            {
                targetView = _service.GetRequiredService<SongListView>();
                if (targetView.DataContext is SongListViewModel vm)
                {
                    vm.LoadSongsByBook(bookId);
                }

                // Update active book indicator
                try
                {
                    using var ctx = new AppDbContext();
                    var book = ctx.Books.Find(bookId);
                    if (book != null)
                    {
                        ActiveBookTitle = book.Title;
                        IsBookSelected = true;
                    }
                    else
                    {
                        ActiveBookTitle = string.Empty;
                        IsBookSelected = false;
                    }
                }
                catch { }

                CurrentView = targetView;
            }

            // Always notify all CreateSongViewModel instances to recalculate next song number
            // This ensures the number is updated regardless of which view is currently active
            //BORRAR SI NO FUNCIONA
            try
            {
                var createSongView = _service.GetService<CreateSongView>();
                if (createSongView?.DataContext is CreateSongViewModel cvm)
                {
                    cvm.RecalculateNextSongNumber();
                }
            }
            catch { }
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
