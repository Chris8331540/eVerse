using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace eVerse.Models
{
    public class Setting : ObservableObject
    {
        public int Id { get; set; }

        // Foreign key to Song
        public int SongId { get; set; }
        public Song? Song { get; set; }

        // Use fade when projecting
        private bool _useFade = true;
        public bool UseFade
        {
            get => _useFade;
            set => SetProperty(ref _useFade, value);
        }

        private int _fadeMs =350;
        public int FadeMs
        {
            get => _fadeMs;
            set => SetProperty(ref _fadeMs, value);
        }

        private string _fontFamily = "Segoe UI";
        public string FontFamily
        {
            get => _fontFamily;
            set => SetProperty(ref _fontFamily, value);
        }

        private bool _autoFit = true;
        public bool AutoFit
        {
            get => _autoFit;
            set => SetProperty(ref _autoFit, value);
        }

        private double _fontSize =56;
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }
    }
}
