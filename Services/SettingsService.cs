using eVerse.Data;
using eVerse.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eVerse.Services
{
 public class SettingsService
 {
 private readonly IDbContextFactory<AppDbContext> _contextFactory;

 public SettingsService(IDbContextFactory<AppDbContext> contextFactory)
 {
 _contextFactory = contextFactory;
 }

 public Setting? GetBySongId(int songId)
 {
 using var context = _contextFactory.CreateDbContext();
 return context.Settings.FirstOrDefault(s => s.SongId == songId);
 }

 public void UpsertSetting(Setting setting)
 {
 using var context = _contextFactory.CreateDbContext();
 var existing = context.Settings.FirstOrDefault(s => s.SongId == setting.SongId);
 if (existing == null)
 {
 context.Settings.Add(setting);
 }
 else
 {
 existing.FontFamily = setting.FontFamily;
 existing.FontSize = setting.FontSize;
 existing.AutoFit = setting.AutoFit;
 existing.UseFade = setting.UseFade;
 existing.FadeMs = setting.FadeMs;
 }

 context.SaveChanges();
 }
 }
}
