using eVerse.Data;
using eVerse.Models;
using Microsoft.EntityFrameworkCore;

namespace eVerse.Services
{
    /// <summary>
    /// Centralized service for accessing and persisting AppConfig settings.
    /// </summary>
    public class AppConfigService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public AppConfigService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Gets the last opened book ID from AppConfig.
        /// Returns null if no book is configured or if the value is <= 0.
        /// </summary>
        public int? GetLastOpenedBookId()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var cfg = context.AppConfigs.FirstOrDefault();
                if (cfg != null && cfg.LastOpenedBook > 0)
                {
                    return cfg.LastOpenedBook;
                }
            }
            catch
            {
                // Ignore DB read errors, return null
            }

            return null;
        }

        /// <summary>
        /// Gets the Book entity for the last opened book.
        /// Returns null if no book is configured or not found.
        /// </summary>
        public Book? GetLastOpenedBook()
        {
            try
            {
                var bookId = GetLastOpenedBookId();
                if (bookId.HasValue)
                {
                    using var context = _contextFactory.CreateDbContext();
                    return context.Books.Find(bookId.Value);
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
        }

        /// <summary>
        /// Sets the last opened book ID in AppConfig.
        /// Creates the AppConfig record if it doesn't exist.
        /// </summary>
        public void SetLastOpenedBookId(int bookId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var cfg = context.AppConfigs.FirstOrDefault();
                if (cfg == null)
                {
                    cfg = new AppConfig { LastOpenedBook = bookId };
                    context.AppConfigs.Add(cfg);
                }
                else
                {
                    cfg.LastOpenedBook = bookId;
                    context.AppConfigs.Update(cfg);
                }
                context.SaveChanges();
            }
            catch
            {
                // Ignore save errors
            }
        }

        /// <summary>
        /// Clears the last opened book (sets to 0/null state).
        /// </summary>
        public void ClearLastOpenedBook()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var cfg = context.AppConfigs.FirstOrDefault();
                if (cfg != null)
                {
                    cfg.LastOpenedBook = 0;
                    context.AppConfigs.Update(cfg);
                    context.SaveChanges();
                }
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
