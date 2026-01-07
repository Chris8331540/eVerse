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
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public SongService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // Crear canción con versos
        public void CreateSong(Song song)
        {
            using var context = _contextFactory.CreateDbContext();
            context.Songs.Add(song);
            context.SaveChanges();
        }

        // Crear canción y opcionalmente asignarla a un Book (por Id)
        public void CreateSong(Song song, int? bookId)
        {
            using var context = _contextFactory.CreateDbContext();
            if (bookId.HasValue)
            {
                var book = context.Books.Find(bookId.Value);
                if (book != null)
                {
                    song.Books.Add(book);
                }
            }

            context.Songs.Add(song);
            context.SaveChanges();
        }

        // Obtener todas las canciones
        public List<Song> GetAllSongs()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var cfg = context.AppConfigs.FirstOrDefault();
                if (cfg != null && cfg.LastOpenedBook > 0)
                {
                    var bookId = cfg.LastOpenedBook;
                    // Query the join table "BookSong" to get song ids for the book
                    var bookSongSet = context.Set<Dictionary<string, object>>("BookSong");
                    var songIds = bookSongSet
                        .Where(bs => EF.Property<int>(bs, "BookId") == bookId)
                        .Select(bs => EF.Property<int>(bs, "SongId"))
                        .ToList();

                    return context.Songs
                        .Include(s => s.Verses)
                        .Where(s => songIds.Contains(s.Id))
                        .ToList();
                }
            }
            catch
            {
                // ignore and fallback to returning all songs
            }

            using var fallbackContext = _contextFactory.CreateDbContext();
            return fallbackContext.Songs.Include(s => s.Verses).ToList();
        }

        public List<Song> GetSongsByBook(int bookId)
        {
            using var context = _contextFactory.CreateDbContext();
            // Use join table to fetch song ids related to the book for correctness
            var bookSongSet = context.Set<Dictionary<string, object>>("BookSong");
            var songIds = bookSongSet
                .Where(bs => EF.Property<int>(bs, "BookId") == bookId)
                .Select(bs => EF.Property<int>(bs, "SongId"))
                .ToList();

            return context.Songs
                .Include(s => s.Verses)
                .Where(s => songIds.Contains(s.Id))
                .ToList();
        }

        // Actualizar canción
        public void UpdateSong(Song song)
        {
            using var context = _contextFactory.CreateDbContext();
            context.Songs.Update(song);
            context.SaveChanges();
        }

        // Borrar canción
        public void DeleteSong(int songId)
        {
            using var context = _contextFactory.CreateDbContext();
            var song = context.Songs.Include(s => s.Verses).FirstOrDefault(s => s.Id == songId);
            if (song != null)
            {
                context.Songs.Remove(song);
                context.SaveChanges();
            }
        }
    }

}

