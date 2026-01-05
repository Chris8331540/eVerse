using eVerse.Models;
using eVerse.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Wpf.Ui.Input;

namespace eVerse.ViewModels
{
    public class CreateSongViewModel : INotifyPropertyChanged
    {
        private readonly SongService _songService;
        public ObservableCollection<VerseViewModel> Verses { get; set; }

        private string songName = string.Empty;
        private int songNumber;
        private int? _editingSongId; // null when creating new

        public string SongName
        {
            get => songName;
            set { songName = value; OnPropertyChanged(); }
        }
        public int SongNumber
        {
            get => songNumber;
            set { songNumber = value; OnPropertyChanged(); }
        }
        public ICommand AddVerseCommand { get; }
        public ICommand SaveSongCommand { get; }
        public ICommand RemoveVerseCommand { get; }

        // Simple message display (OK)
        public Action<string>? ShowMessage { get; set; }
        // Confirmation dialog: should return true if user accepts
        public Func<string, bool>? AskConfirmation { get; set; }

        public CreateSongViewModel(SongService songService)
        {
            _songService = songService;
            // Por defecto agregamos1 bloque de estrofa
            Verses = new ObservableCollection<VerseViewModel>
            {
            new VerseViewModel(new Verse()) // Bloque inicial
            };

            AddVerseCommand = new RelayCommand(AddVerse);
            RemoveVerseCommand = new RelayCommand<VerseViewModel>(RemoveVerse);
            SaveSongCommand = new RelayCommand(SaveSong);

            // Inicializar SongNumber por defecto: siguiente número lógico
            ResetToDefaultState();
        }

        private void AddVerse()
        {
            Verses.Add(new VerseViewModel(new Verse()));
        }

        private void SaveSong()
        {
            // Validaciones
            if (SongNumber <=0)
            {
                ShowMessage?.Invoke("El número de canción debe ser un número positivo.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SongName))
            {
                ShowMessage?.Invoke("El título de la canción no puede estar vacío.");
                return;
            }

            // Debe tener al menos1 estrofa no vacía
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
                Order = index +1
            })
            .ToList();

            // Buscar conflicto por SongNumber
            var existing = _songService.GetAllSongs().FirstOrDefault(s => s.SongNumber == this.SongNumber);

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
                var toUpdate = _songService.GetAllSongs().FirstOrDefault(s => s.Id == _editingSongId.Value);
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

            _songService.CreateSong(song);
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
                Verses.Add(new VerseViewModel(new Verse())
                {
                    Text = verse.Text
                });
            }

            // Si por algún motivo no hubiera estrofas, creamos al menos1 vacía
            if (Verses.Count ==0)
                AddVerse();
        }

        private void RemoveVerse(VerseViewModel verse)
        {
            if (verse != null)
                Verses.Remove(verse);
        }

        private void ResetToDefaultState()
        {
            // Reset fields
            SongName = string.Empty;
            Verses.Clear();
            Verses.Add(new VerseViewModel(new Verse()));
            _editingSongId = null;

            // Recalculate next SongNumber
            try
            {
                var all = _songService.GetAllSongs();
                if (all != null && all.Count >0)
                {
                    SongNumber = all.Max(s => s.SongNumber) +1;
                }
                else
                {
                    SongNumber =1;
                }
            }
            catch
            {
                SongNumber =1;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
