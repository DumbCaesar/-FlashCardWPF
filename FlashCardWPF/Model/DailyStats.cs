using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlashCardWPF.Model
{
    public class DailyStats
    {
        // Date the stats belong to
        public string Date { get; set; }

        // Stored raw seconds for JSON serialization
        public double TotalStudyTimeSeconds { get; set; }

        // Amount of cards reviewed this day
        public int TotalCards { get; set; }

        [JsonIgnore]
        // Helper property for converting stored seconds into TimeSpan
        public TimeSpan TotalStudyTime
        {
            get => TimeSpan.FromSeconds(TotalStudyTimeSeconds);
            set => TotalStudyTimeSeconds = value.TotalSeconds;
        }

        [JsonIgnore]
        // Auto calculates the average time spent per card studied
        public TimeSpan AverageTimePerCard => 
            TotalCards > 0 ? TimeSpan.FromSeconds(TotalStudyTimeSeconds / TotalCards) 
            : TimeSpan.Zero;
    } 
}
