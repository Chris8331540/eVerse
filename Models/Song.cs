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
        public string Title { get; set; } = string.Empty;
        public int SongNumber { get; set; }

        //property navigation to Verses
        public ICollection<Verse> Verses { get; set; } = new List<Verse>();

        // Per-song settings
        public Setting? Setting { get; set; }
        public int? SettingId { get; set; }
    }
}
