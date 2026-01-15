using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eVerse.Models;
using eVerse.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace eVerse.ViewModels
{
    public partial class SongListViewModel : ObservableObject
    {
        private readonly ProjectionService _projectionService;
        private readonly ProjectionSettings _projectionSettings;
        private readonly AppConfigService _appConfigService;

        // Exponer settings para binding
        public ProjectionSettings ProjectionSettings => _projectionSettings;

        private readonly SongService _songService;
        private readonly IServiceProvider _serviceProvider;

        private ICollectionView? _songsView;
        public ICollectionView? SongsView
        {
            get => _songsView!;
            private set => SetProperty(ref _songsView, value);
        }

        //Propiedad observable para buscar segun numero de cancion
        [ObservableProperty]
        private string searchText = string.Empty;

        // Lista observable de canciones
        [ObservableProperty]
        private ObservableCollection<Song> songs = new();

        // Texto actualmente proyectado (para resaltar verso en UI)
        [ObservableProperty]
        private string projectedText = string.Empty;

        // Canción seleccionada en la UI
        [ObservableProperty]
        private Song? selectedSong;

        // Edits buffer for the selected song title (so changes are not applied to the model until Save)
        [ObservableProperty]
        private string selectedSongTitle = string.Empty;

        // Versos de la canción seleccionada (colección observable para editar visualmente)
        private ObservableCollection<Verse>? _selectedSongVerses;
        public ObservableCollection<Verse>? SelectedSongVerses
        {
            get => _selectedSongVerses;
            private set => SetProperty(ref _selectedSongVerses, value);
        }

        // Lista de fuentes disponibles (simple ejemplo)
        public ObservableCollection<string> AvailableFonts { get; } = new ObservableCollection<string>
    {
        "Segoe UI", "Arial", "Times New Roman", "Calibri", "Verdana"
    };

        public SongListViewModel(SongService songService, ProjectionService projectionService, ProjectionSettings settings, IServiceProvider serviceProvider, AppConfigService appConfigService)
        {
            _songService = songService;
            _projectionService = projectionService;
            _projectionSettings = settings;
            _serviceProvider = serviceProvider;
            _appConfigService = appConfigService;

            // Load songs according to AppConfig last opened book if present
            var bookId = _appConfigService.GetLastOpenedBookId();
            if (bookId.HasValue)
            {
                LoadSongsByBook(bookId.Value);
            }

            // Subscribe to projection text changes to update UI highlight
            try
            {
                _projectionService.ProjectedTextChanged += text => ProjectedText = text ?? string.Empty;
            }
            catch { }
        }

        partial void OnSelectedSongChanged(Song? oldValue, Song? newValue)
        {
            // Update ProjectionSettings SongId so per-song settings are loaded/saved
            _projectionSettings.SongId = newValue?.Id;

            // Populate observable verses collection for editing in the UI
            if (newValue == null)
            {
                SelectedSongVerses = null;
                SelectedSongTitle = string.Empty;
            }
            else
            {
                // Use a copy of the verses so edits do not modify the original until saved
                var ordered = newValue.Verses?.OrderBy(v => v.Order).ToList() ?? new List<Verse>();
                var clones = ordered.Select(v => new Verse
                {
                    Id = v.Id,
                    SongId = v.SongId,
                    Order = v.Order,
                    Text = v.Text
                }).ToList();
                SelectedSongVerses = new ObservableCollection<Verse>(clones);

                // Copy title into editable buffer
                SelectedSongTitle = newValue.Title ?? string.Empty;
            }

            // If a projection is currently showing some text, automatically show the first verse of the newly selected song
            try
            {
                if (newValue != null && _projectionService.CurrentProjectedText != null && _projectionService.CurrentProjectedText.Length > 0)
                {
                    var first = newValue.Verses?.OrderBy(v => v.Order).FirstOrDefault()?.Text;
                    if (!string.IsNullOrWhiteSpace(first))
                    {
                        _projectionService.ShowProjection(first);
                    }
                }
            }
            catch { }

            // Force command CanExecute re-evaluation so AddVerse button updates enabled state
            try
            {
                if (AddVerseCommand is IRelayCommand relay)
                    relay.NotifyCanExecuteChanged();
            }
            catch { }
        }

        //// Cargar canciones desde la BD
        //[RelayCommand]
        //private void LoadSongs()
        //{
        //    Songs = new ObservableCollection<Song>(_songService.GetAllSongs());
        //    SongsView = CollectionViewSource.GetDefaultView(Songs);
        //    SongsView.Filter = SongsFilter;
        //}

        public void LoadSongsByBook(int bookId)
        {
            Songs = new ObservableCollection<Song>(_songService.GetSongsByBook(bookId));
            SongsView = CollectionViewSource.GetDefaultView(Songs);
            SongsView.Filter = SongsFilter;
            SelectedSong = Songs.FirstOrDefault();
        }

        // Eliminar canción seleccionada
        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void DeleteSong()
        {
            if (SelectedSong == null)
                return;

            _songService.DeleteSong(SelectedSong.Id);
            var bookId = _appConfigService.GetLastOpenedBookId();
            if (bookId.HasValue)
            {
                LoadSongsByBook(bookId.Value);
            }
        }

        private bool CanDelete()
        {
            return SelectedSong != null;
        }

        // Añadir estrofa visualmente a la canción seleccionada
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void AddVerse()
        {
            if (SelectedSong == null) return;
            if (SelectedSongVerses == null) SelectedSongVerses = new ObservableCollection<Verse>();
            SelectedSongVerses.Add(new Verse { Text = string.Empty, Order = (SelectedSongVerses.Count + 1) });
        }

        // Eliminar estrofa visualmente (no persiste hasta guardar)
        [RelayCommand]
        private void RemoveVerse(Verse verse)
        {
            if (SelectedSongVerses == null || verse == null) return;
            SelectedSongVerses.Remove(verse);
            // Recompute orders
            for (int i = 0; i < SelectedSongVerses.Count; i++)
                SelectedSongVerses[i].Order = i + 1;
        }

        [RelayCommand]
        private void ShowVerse(string verseText)
        {
            if (string.IsNullOrWhiteSpace(verseText)) return;

            try
            {
                var current = _projectionService.CurrentProjectedText ?? string.Empty;
                if (string.Equals(current, verseText, StringComparison.Ordinal))
                {
                    // If the clicked verse is currently projected, clear the text (leave window open)
                    _projectionService.ClearProjectedText();
                }
                else
                {
                    _projectionService.ShowProjection(verseText);
                }
            }
            catch
            {
                // fallback to show projection
                _projectionService.ShowProjection(verseText);
            }
        }

        // Nuevo: comando para guardar cambios en una canción. Recibe un objeto Song que contiene el título y las estrofas (Verses).
        [RelayCommand]
        private void SaveSong(Song? song)
        {
            if (song == null) return;

            // Apply buffered title and verses back into the song and persist
            song.Title = SelectedSongTitle;
            if (SelectedSongVerses != null)
            {
                song.Verses = SelectedSongVerses.Select(v => new Verse
                {
                    Id = v.Id,
                    Text = v.Text,
                    Order = v.Order,
                    SongId = song.Id
                }).ToList();
            }

            // Usa el servicio para actualizar la entidad en la base de datos
            _songService.UpdateSong(song);

            // Recargar la lista para reflejar cambios (opcional)
            var bookId = _appConfigService.GetLastOpenedBookId();
            if (bookId.HasValue)
            {
                LoadSongsByBook(bookId.Value);
            }

            // Mantener la selección en la canción guardada, si existe
            var kept = Songs.FirstOrDefault(s => s.Id == song.Id);
            if (kept != null)
                SelectedSong = kept;

            // Informar al usuario
            System.Windows.MessageBox.Show("Cambios guardados correctamente.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private bool CanEdit()
        {
            return SelectedSong != null;
        }
        private bool SongsFilter(object obj)
        {
            if (obj is not Song song)
                return false;
            if (string.IsNullOrWhiteSpace(SearchText))
                return true; // Sin filtro muestra todo

            var query = SearchText.Trim();

            // 1) If the full query is a number, match SongNumber
            if (int.TryParse(query, out var qnum))
            {
                if (song.SongNumber == qnum) return true;
            }

            // 2) Check title contains the full query (case-insensitive)
            if (!string.IsNullOrEmpty(song.Title) && song.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // 3) Check verses: if any verse contains the full query
            if (song.Verses != null)
            {
                foreach (var v in song.Verses)
                {
                    if (!string.IsNullOrEmpty(v.Text) && v.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            // 4) As a fallback, split into tokens and match any token against number/title/verses
            var separators = new char[] { ',', ' ' };
            var parts = query.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var numPart) && song.SongNumber == numPart)
                    return true;

                if (!string.IsNullOrEmpty(song.Title) && song.Title.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (song.Verses != null)
                {
                    foreach (var v in song.Verses)
                    {
                        if (!string.IsNullOrEmpty(v.Text) && v.Text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                }
            }

            return false;
        }

        partial void OnSearchTextChanged(string oldValue, string newValue)
        {
            SongsView?.Refresh();
        }

    }
}
