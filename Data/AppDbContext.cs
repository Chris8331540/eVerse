using eVerse.Models;
using Microsoft.EntityFrameworkCore;

namespace eVerse.Data
{
    public class AppDbContext : DbContext
    {
        // Tablas
        public DbSet<Song> Songs { get; set; } = null!;
        public DbSet<Verse> SongVerses { get; set; } = null!;

        // Configuración de conexión
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Cambia el Data Source y credenciales según tu servidor
            options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=eVerseDB;Trusted_Connection=True;");
        }

        // Configuración de relaciones
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>()
                .HasMany(s => s.Verses)
                .WithOne(v => v.Song)
                .HasForeignKey(v => v.SongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Verse>()
                .HasOne(v => v.Song)
                .WithMany(s => s.Verses)
                .HasForeignKey(v => v.SongId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
