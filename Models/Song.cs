using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eVerse.Models
{
    public class Song
    {

        public int Id { get; set; }
        public string Title { get; set; }
        public int SongNumber { get; set; }

        //property navigation to Verses
        public ICollection<Verse> Verses { get; set; } = new List<Verse>();

    }
}
