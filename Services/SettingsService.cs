using eVerse.Data;
using eVerse.Models;
using System.Linq;

namespace eVerse.Services
{
 public class SettingsService
 {
 private readonly AppDbContext _context;

 public SettingsService(AppDbContext context)
 {
 _context = context;
 }

 public Setting? GetBySongId(int songId)
 {
 return _context.Settings.FirstOrDefault(s => s.SongId == songId);
 }

 public void UpsertSetting(Setting setting)
 {
 var existing = _context.Settings.FirstOrDefault(s => s.SongId == setting.SongId);
 if (existing == null)
 {
 _context.Settings.Add(setting);
 }
 else
 {
 existing.FontFamily = setting.FontFamily;
 existing.FontSize = setting.FontSize;
 existing.AutoFit = setting.AutoFit;
 existing.UseFade = setting.UseFade;
 existing.FadeMs = setting.FadeMs;
 }

 _context.SaveChanges();
 }
 }
}
