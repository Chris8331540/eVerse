using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using eVerse.Models;

namespace eVerse.ViewModels
{
    public class VerseViewModel : INotifyPropertyChanged
    {
        private readonly Verse model;

        // Constructor recibe el modelo
        public VerseViewModel(Verse verseModel)
        {
            model = verseModel;
        }


        private string text;
        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    model.Text = value; // Sincronizamos con el modelo
                    OnPropertyChanged();
                }
            }
        }

        // Método para obtener el modelo subyacente cuando se guarda
        public Verse GetModel() => model;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
