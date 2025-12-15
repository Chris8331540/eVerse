using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eVerse.Models
{
    public class Verse
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public int Order { get; set; }

        public int SongId { get; set; }


        //navigation property to Song
        public Song Song { get; set; }
    }
}
