using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eVerse.Models;
using eVerse.Services;
using System.Collections.ObjectModel;

namespace eVerse.ViewModels
{
    public partial class CreateSongViewModel : ObservableObject
    {
        private readonly SongService _songService;
        private readonly AppConfigService _appConfigService;
        
        public ObservableCollection<VerseViewModel> Verses { get; set; }

        [ObservableProperty]
        private string songName = string.Empty;

        [ObservableProperty]
        private int songNumber;

        private int? _editingSongId; // null when creating new

        public IRelayCommand AddVerseCommand { get; }
        public IRelayCommand SaveSongCommand { get; }
        public IRelayCommand<VerseViewModel> RemoveVerseCommand { get; }

        // Simple message display (OK)
        public Action<string>? ShowMessage { get; set; }
        // Confirmation dialog: should return true if user accepts
        public Func<string, bool>? AskConfirmation { get; set; }

        public CreateSongViewModel(SongService songService, AppConfigService appConfigService)
        {
            _songService = songService;
            _appConfigService = appConfigService;
            
            // Por defecto agregamos 1 bloque de estrofa
            Verses = new ObservableCollection<VerseViewModel>
            {
                new VerseViewModel() // Bloque inicial
            };

            AddVerseCommand = new RelayCommand(AddVerse);
            RemoveVerseCommand = new RelayCommand<VerseViewModel>(RemoveVerse);
            SaveSongCommand = new RelayCommand(SaveSong);

            // Inicializar SongNumber por defecto: siguiente número lógico
            ResetToDefaultState();
        }

        // Recalculate next SongNumber according to currently active Book (AppConfig)
        public void RecalculateNextSongNumber()
        {
            try
            {
                var bookId = _appConfigService.GetLastOpenedBookId();
                if (bookId.HasValue)
                {
                    var all = _songService.GetSongsByBook(bookId.Value);
                    SongNumber = (all != null && all.Count > 0) ? all.Max(s => s.SongNumber) + 1 : 1;
                }
                else
                {
                    SongNumber = 1;
                }
            }
            catch
            {
                SongNumber = 1;
            }
        }

        private void AddVerse()
        {
            Verses.Add(new VerseViewModel());
        }

        private void SaveSong()
        {
            // Ensure there is an active Book configured
            int? activeBookId = _appConfigService.GetLastOpenedBookId();

            if (!activeBookId.HasValue)
            {
                var result = System.Windows.MessageBox.Show("No hay ningún cuaderno abierto. ¿Deseas crear uno ahora?", "Crear cuaderno", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (result != System.Windows.MessageBoxResult.Yes)
                    return;

                // Open CreateBookWindow
                var createDlg = new Views.CreateBookWindow();
                createDlg.Owner = System.Windows.Application.Current?.MainWindow;
                if (createDlg.ShowDialog() != true || createDlg.CreatedBook == null)
                {
                    // user cancelled
                    return;
                }

                // Persist selection into AppConfig
                _appConfigService.SetLastOpenedBookId(createDlg.CreatedBook.Id);
                activeBookId = createDlg.CreatedBook.Id;

                // Update main window VM indicator and load songs for the new book
                try
                {
                    if (System.Windows.Application.Current?.MainWindow?.DataContext is MainWindowViewModel mwvm)
                    {
                        mwvm.LoadSongsForBook(createDlg.CreatedBook.Id);
                    }
                }
                catch { }
            }

            // Validaciones
            if (SongNumber <= 0)
            {
                ShowMessage?.Invoke("El número de canción debe ser un número positivo.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SongName))
            {
                ShowMessage?.Invoke("El título de la canción no puede estar vacío.");
                return;
            }

            // Debe tener al menos 1 estrofa no vacía
            var hasNonEmptyVerse = Verses != null && Verses.Any(v => !string.IsNullOrWhiteSpace(v.Text));
            if (!hasNonEmptyVerse)
            {
                ShowMessage?.Invoke("La canción debe tener al menos una estrofa con texto.");
                return;
            }

            // Preparar lista de versos desde ViewModels
            var verses = this.Verses
                .Select((vm, index) => new Verse
                {
                    Text = vm.Text,
                    Order = index + 1
                })
                .ToList();

            // Get songs from active book to check for conflicts
            var songsInBook = _songService.GetSongsByBook(activeBookId!.Value);

            // Buscar conflicto por SongNumber
            var existing = songsInBook.FirstOrDefault(s => s.SongNumber == this.SongNumber);

            if (existing != null)
            {
                // If editing an existing song, and it's the same record, just update it
                if (_editingSongId.HasValue && existing.Id == _editingSongId.Value)
                {
                    existing.Title = this.SongName;
                    existing.Verses = verses;
                    _songService.UpdateSong(existing);
                    ShowMessage?.Invoke("Canción actualizada correctamente.");

                    // Reset view to initial state
                    ResetToDefaultState();
                    return;
                }

                // Conflict with a different record -> ask user
                var confirm = AskConfirmation?.Invoke($"Ya existe una canción con el número {SongNumber}. ¿Deseas sobrescribirla?");
                if (confirm != true)
                {
                    // User cancelled: do nothing
                    return;
                }

                // User accepted -> overwrite the existing record with new data
                existing.Title = this.SongName;
                existing.Verses = verses;
                _songService.UpdateSong(existing);

                // If we were editing another song (i.e. renumbering current song to overwrite someone else), remove the original
                if (_editingSongId.HasValue && _editingSongId.Value != existing.Id)
                {
                    // delete the original song being edited
                    _songService.DeleteSong(_editingSongId.Value);
                }

                ShowMessage?.Invoke("Canción sobrescrita correctamente.");

                // Reset view to initial state
                ResetToDefaultState();
                return;
            }

            // No conflict: if editing existing song, update it; else create new
            if (_editingSongId.HasValue)
            {
                var toUpdate = songsInBook.FirstOrDefault(s => s.Id == _editingSongId.Value);
                if (toUpdate != null)
                {
                    toUpdate.Title = this.SongName;
                    toUpdate.SongNumber = this.SongNumber;
                    toUpdate.Verses = verses;
                    _songService.UpdateSong(toUpdate);
                    ShowMessage?.Invoke("Canción actualizada correctamente.");

                    // Reset view to initial state
                    ResetToDefaultState();
                    return;
                }
            }

            // Create new song
            var song = new Song
            {
                Title = this.SongName,
                SongNumber = this.SongNumber,
                Verses = verses
            };

            // Persist song and assign to currently opened book
            _songService.CreateSong(song, activeBookId);

            ShowMessage?.Invoke("Canción creada correctamente.");

            // Reset view to initial state
            ResetToDefaultState();
        }

        public void LoadFromSong(Song song)
        {
            if (song == null)
                return;

            // Asignar propiedades principales
            SongName = song.Title;
            SongNumber = song.SongNumber;
            _editingSongId = song.Id;

            // Limpiar estrofas anteriores si existían
            Verses.Clear();

            // Insertar las estrofas cargadas desde la BD
            foreach (var verse in song.Verses.OrderBy(v => v.Order))
            {
                Verses.Add(new VerseViewModel
                {
                    Text = verse.Text
                });
            }

            // Si por algún motivo no hubiera estrofas, creamos al menos 1 vacía
            if (Verses.Count == 0)
                AddVerse();
        }

        private void RemoveVerse(VerseViewModel? verse)
        {
            if (verse != null)
                Verses.Remove(verse);
        }

        private void ResetToDefaultState()
        {
            // Reset fields
            SongName = string.Empty;
            Verses.Clear();
            Verses.Add(new VerseViewModel());
            _editingSongId = null;

            // Recalculate next SongNumber
            RecalculateNextSongNumber();
        }
    }
}
