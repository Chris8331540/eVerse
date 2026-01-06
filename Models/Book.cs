using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eVerse.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        // Navigation: a book can contain many songs
        public ICollection<Song> Songs { get; set; } = new List<Song>();
    }
}
