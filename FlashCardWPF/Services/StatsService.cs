using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using FlashCardWPF.Model;

namespace FlashCardWPF.Services
{
    public class StatsService
    {
        private readonly string _statsFilePath;
        private const string STATS_FILENAME = "daily_stats.json";

        public StatsService()
        {
            string baseDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                ".."
            );
            string dataDir = Path.Combine(baseDir, "Data");
            Directory.CreateDirectory(dataDir);
            _statsFilePath = Path.Combine(dataDir, STATS_FILENAME);
        }

        public DailyStats LoadTodayStats()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");

            if (File.Exists(_statsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_statsFilePath);
                    var stats = JsonSerializer.Deserialize<DailyStats>(json);

                    if (stats?.Date == today)
                    {
                        Debug.WriteLine(
                            $"Loaded today's stats: {stats.TotalCards} cards, " +
                            $"{stats.TotalStudyTime:mm\\:ss} study time"
                        );
                        return stats;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading stats: {ex.Message}");
                }
            }

            // Return new stats for today
            return new DailyStats { Date = today };
        }

        public void SaveStats(DailyStats stats)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(stats, options);
            File.WriteAllText(_statsFilePath, json);
            Debug.WriteLine($"Stats saved: {stats.TotalCards} cards reviewed");
        }

        public void UpdateStats(int cardsReviewed, TimeSpan sessionTime)
        {
            var stats = LoadTodayStats();
            stats.TotalCards += cardsReviewed;
            stats.TotalStudyTimeSeconds += sessionTime.TotalSeconds;
            SaveStats(stats);
        }
    }
}