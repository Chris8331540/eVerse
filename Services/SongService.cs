using eVerse.Data;
using eVerse.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eVerse.Services
{
    public class SongService
    {
        private readonly AppDbContext _context;

        public SongService(AppDbContext context)
        {
            _context = context;
        }

        // Crear canción con versos
        public void CreateSong(Song song)
        {
            _context.Songs.Add(song);
            _context.SaveChanges();
        }

        // Obtener todas las canciones
        public List<Song> GetAllSongs()
        {
            return _context.Songs.Include(s => s.Verses).ToList();
        }

        // Actualizar canción
        public void UpdateSong(Song song)
        {
            _context.Songs.Update(song);
            _context.SaveChanges();
        }

        // Borrar canción
        public void DeleteSong(int songId)
        {
            var song = _context.Songs.Include(s => s.Verses).FirstOrDefault(s => s.Id == songId);
            if (song != null)
            {
                _context.Songs.Remove(song);
                _context.SaveChanges();
            }
        }
    }

}

